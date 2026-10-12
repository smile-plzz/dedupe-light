using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;

namespace DedupeLight;

static class DuplicateScanner
{
    sealed record CacheEntry(long Size, long Ticks, string Hash);
    public static async Task<ScanResult> FindAsync(string root, string cachePath, ScanOptions options, IProgress<string> progress, CancellationToken token)
    {
        var cache = Load(cachePath); var files = new List<FileInfo>(); var pending = new Stack<string>(); pending.Push(root); var skipped = 0;
        while (pending.Count > 0)
        {
            token.ThrowIfCancellationRequested(); var dir = pending.Pop();
            try
            {
                foreach (var directory in Directory.EnumerateDirectories(dir)) try { var info = new DirectoryInfo(directory); if (!options.IncludeHiddenSystem && (info.Attributes & (FileAttributes.Hidden | FileAttributes.System)) != 0) continue; if ((info.Attributes & FileAttributes.ReparsePoint) == 0) pending.Push(directory); } catch { skipped++; }
                foreach (var file in Directory.EnumerateFiles(dir)) try { var info = new FileInfo(file); if (info.Length < options.MinimumBytes) continue; if (!options.IncludeHiddenSystem && (info.Attributes & (FileAttributes.Hidden | FileAttributes.System)) != 0) continue; files.Add(info); } catch { skipped++; }
            }
            catch { skipped++; }
            if (files.Count % 500 < 30) progress.Report($"Discovering files  •  {files.Count:N0} found");
        }
        var candidates = files.GroupBy(f => f.Length).Where(g => g.Count() > 1).SelectMany(g => g).ToList(); progress.Report($"Hashing candidates  •  0 / {candidates.Count:N0}"); var done = 0;
        var hashed = new ConcurrentBag<(FileInfo file, string hash)>(); var updated = new ConcurrentDictionary<string, CacheEntry>(cache, StringComparer.OrdinalIgnoreCase);
        await Parallel.ForEachAsync(candidates, new ParallelOptions { CancellationToken = token, MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount / 2) }, async (file, ct) =>
        {
            try
            {
                string hash;
                if (updated.TryGetValue(file.FullName, out var entry) && entry.Size == file.Length && entry.Ticks == file.LastWriteTimeUtc.Ticks) hash = entry.Hash;
                else { await using var stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan); hash = Convert.ToHexString(await SHA256.HashDataAsync(stream, ct)); updated[file.FullName] = new(file.Length, file.LastWriteTimeUtc.Ticks, hash); }
                hashed.Add((file, hash));
            }
            catch when (!ct.IsCancellationRequested) { Interlocked.Increment(ref skipped); }
            var count = Interlocked.Increment(ref done); if (count % 20 == 0 || count == candidates.Count) progress.Report($"Hashing candidates  •  {count:N0} / {candidates.Count:N0}");
        });
        Save(cachePath, updated);
        var groups = hashed.GroupBy(x => (x.file.Length, x.hash)).Where(g => g.Count() > 1).OrderByDescending(g => g.Key.Length * (g.Count() - 1)).ToList(); var result = new List<DuplicateItem>(); var id = 1;
        foreach (var group in groups) { foreach (var item in group.OrderBy(x => x.file.FullName)) result.Add(new DuplicateItem { Group = id, Path = item.file.FullName, Hash = item.hash, Size = item.file.Length, ModifiedUtc = item.file.LastWriteTimeUtc }); id++; }
        return new(result, files.Count, candidates.Count, skipped);
    }
    static Dictionary<string, CacheEntry> Load(string path) { try { return JsonSerializer.Deserialize<Dictionary<string, CacheEntry>>(File.ReadAllText(path)) ?? new(StringComparer.OrdinalIgnoreCase); } catch { return new(StringComparer.OrdinalIgnoreCase); } }
    static void Save(string path, ConcurrentDictionary<string, CacheEntry> cache) { try { Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllText(path, JsonSerializer.Serialize(cache.Where(x => File.Exists(x.Key)).ToDictionary())); } catch { } }
}
