using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;

namespace DedupeLight;

public static class DuplicateScanner
{
    const int SampleBlockSize = 64 * 1024;
    sealed record CacheEntry(long Size, long Ticks, string? SampleHash, string? FullHash);
    sealed class ScanCounters { public int Skipped; }

    public static async Task<ScanResult> FindAsync(string root, string cachePath, ScanOptions options, IProgress<string> progress, CancellationToken token)
    {
        var counters = new ScanCounters();
        var cache = Load(cachePath);
        var updated = new ConcurrentDictionary<string, CacheEntry>(cache, StringComparer.OrdinalIgnoreCase);
        var files = Discover(root, options, progress, token, counters);
        var sizeCandidates = files.GroupBy(f => f.Length).Where(g => g.Count() > 1).SelectMany(g => g).ToList();

        var sampled = new ConcurrentBag<(FileInfo File, string Sample)>();
        var sampledCount = 0;
        progress.Report($"Sampling candidates  •  0 / {sizeCandidates.Count:N0}");
        await Parallel.ForEachAsync(sizeCandidates, ParallelOptions(token), async (file, ct) =>
        {
            try
            {
                CacheEntry? entry;
                string sample;
                if (TryCached(updated, file, out entry) && entry!.SampleHash is not null) sample = entry.SampleHash;
                else
                {
                    sample = await SampleHashAsync(file, ct);
                    updated[file.FullName] = new(file.Length, file.LastWriteTimeUtc.Ticks, sample, entry?.FullHash);
                }
                sampled.Add((file, sample));
            }
            catch when (!ct.IsCancellationRequested) { Interlocked.Increment(ref counters.Skipped); }
            var count = Interlocked.Increment(ref sampledCount);
            if (count % 50 == 0 || count == sizeCandidates.Count) progress.Report($"Sampling candidates  •  {count:N0} / {sizeCandidates.Count:N0}");
        });

        var fullCandidates = sampled.GroupBy(x => (x.File.Length, x.Sample)).Where(g => g.Count() > 1).SelectMany(g => g).ToList();
        var hashed = new ConcurrentBag<(FileInfo File, string Hash)>();
        var fullHashes = 0;
        var cacheHits = 0;
        var completed = 0;
        progress.Report($"Verifying exact matches  •  0 / {fullCandidates.Count:N0}");
        await Parallel.ForEachAsync(fullCandidates, ParallelOptions(token), async (candidate, ct) =>
        {
            try
            {
                string hash;
                if (TryCached(updated, candidate.File, out var entry) && entry!.FullHash is not null)
                {
                    hash = entry.FullHash;
                    Interlocked.Increment(ref cacheHits);
                }
                else if (candidate.File.Length <= SampleBlockSize * 2)
                {
                    hash = candidate.Sample;
                    updated[candidate.File.FullName] = new(candidate.File.Length, candidate.File.LastWriteTimeUtc.Ticks, candidate.Sample, hash);
                    Interlocked.Increment(ref fullHashes);
                }
                else
                {
                    await using var stream = new FileStream(candidate.File.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
                    hash = Convert.ToHexString(await SHA256.HashDataAsync(stream, ct));
                    updated[candidate.File.FullName] = new(candidate.File.Length, candidate.File.LastWriteTimeUtc.Ticks, candidate.Sample, hash);
                    Interlocked.Increment(ref fullHashes);
                }
                hashed.Add((candidate.File, hash));
            }
            catch when (!ct.IsCancellationRequested) { Interlocked.Increment(ref counters.Skipped); }
            var count = Interlocked.Increment(ref completed);
            if (count % 20 == 0 || count == fullCandidates.Count) progress.Report($"Verifying exact matches  •  {count:N0} / {fullCandidates.Count:N0}");
        });

        Save(cachePath, updated);
        var groups = hashed.GroupBy(x => (x.File.Length, x.Hash)).Where(g => g.Count() > 1).OrderByDescending(g => g.Key.Length * (g.Count() - 1));
        var result = new List<DuplicateItem>();
        var groupId = 1;
        foreach (var group in groups)
        {
            foreach (var item in group.OrderBy(x => x.File.FullName)) result.Add(new DuplicateItem { Group = groupId, Path = item.File.FullName, Hash = item.Hash, Size = item.File.Length, ModifiedUtc = item.File.LastWriteTimeUtc });
            groupId++;
        }
        return new(result, files.Count, sizeCandidates.Count, fullHashes, cacheHits, counters.Skipped);
    }

    static List<FileInfo> Discover(string root, ScanOptions options, IProgress<string> progress, CancellationToken token, ScanCounters counters)
    {
        var files = new List<FileInfo>();
        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            token.ThrowIfCancellationRequested();
            var directory = pending.Pop();
            try
            {
                foreach (var child in Directory.EnumerateDirectories(directory))
                {
                    try { var info = new DirectoryInfo(child); if (!options.IncludeHiddenSystem && HasHiddenOrSystem(info.Attributes)) continue; if ((info.Attributes & FileAttributes.ReparsePoint) == 0) pending.Push(child); }
                    catch { counters.Skipped++; }
                }
                foreach (var path in Directory.EnumerateFiles(directory))
                {
                    try { var info = new FileInfo(path); if (info.Length >= options.MinimumBytes && (options.IncludeHiddenSystem || !HasHiddenOrSystem(info.Attributes))) files.Add(info); }
                    catch { counters.Skipped++; }
                }
            }
            catch { counters.Skipped++; }
            if (files.Count % 500 < 30) progress.Report($"Discovering files  •  {files.Count:N0} found");
        }
        return files;
    }

    static async Task<string> SampleHashAsync(FileInfo file, CancellationToken token)
    {
        await using var stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, SampleBlockSize, FileOptions.Asynchronous | FileOptions.RandomAccess);
        if (file.Length <= SampleBlockSize * 2) return Convert.ToHexString(await SHA256.HashDataAsync(stream, token));
        var buffer = new byte[SampleBlockSize * 2];
        await stream.ReadExactlyAsync(buffer.AsMemory(0, SampleBlockSize), token);
        stream.Seek(-SampleBlockSize, SeekOrigin.End);
        await stream.ReadExactlyAsync(buffer.AsMemory(SampleBlockSize, SampleBlockSize), token);
        return Convert.ToHexString(SHA256.HashData(buffer));
    }

    static bool TryCached(ConcurrentDictionary<string, CacheEntry> cache, FileInfo file, out CacheEntry? entry) => cache.TryGetValue(file.FullName, out entry) && entry.Size == file.Length && entry.Ticks == file.LastWriteTimeUtc.Ticks;
    static bool HasHiddenOrSystem(FileAttributes attributes) => (attributes & (FileAttributes.Hidden | FileAttributes.System)) != 0;
    static ParallelOptions ParallelOptions(CancellationToken token) => new() { CancellationToken = token, MaxDegreeOfParallelism = Math.Clamp(Environment.ProcessorCount / 2, 1, 4) };
    static Dictionary<string, CacheEntry> Load(string path) { try { return JsonSerializer.Deserialize<Dictionary<string, CacheEntry>>(File.ReadAllText(path)) ?? new(StringComparer.OrdinalIgnoreCase); } catch { return new(StringComparer.OrdinalIgnoreCase); } }
    static void Save(string path, ConcurrentDictionary<string, CacheEntry> cache) { try { Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllText(path, JsonSerializer.Serialize(cache)); } catch { } }
}
