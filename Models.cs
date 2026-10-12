namespace DedupeLight;

public sealed class DuplicateItem
{
    public bool Selected { get; set; }
    public int Group { get; init; }
    public required string Path { get; init; }
    public required string Hash { get; init; }
    public long Size { get; init; }
    public DateTime ModifiedUtc { get; init; }
    public string Name => System.IO.Path.GetFileName(Path);
    public string Folder => System.IO.Path.GetDirectoryName(Path) ?? "";
    public string SizeText => MainForm.FormatSize(Size);
    public string ModifiedText => ModifiedUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
}

public sealed record ScanOptions(long MinimumBytes, bool IncludeHiddenSystem, IReadOnlyList<ExclusionRule>? Exclusions = null);
public sealed record ScanResult(List<DuplicateItem> Items, int FilesSeen, int SizeCandidates, int FullHashes, int CacheHits, int Skipped);
