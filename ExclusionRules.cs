namespace DedupeLight;

public sealed record ExclusionRule(string Path, bool IsFolder)
{
    public override string ToString() => $"{(IsFolder ? "Folder" : "File")}  ·  {Path}";
}

public static class ExclusionRules
{
    public static string Normalize(string path)
    {
        if (!System.IO.Path.IsPathFullyQualified(path)) throw new ArgumentException("Use an absolute file or folder path.");
        return System.IO.Path.TrimEndingDirectorySeparator(System.IO.Path.GetFullPath(path));
    }

    public static bool Matches(string path, IEnumerable<ExclusionRule>? rules)
    {
        if (rules is null) return false;
        var candidate = Normalize(path);
        foreach (var rule in rules)
        {
            var excluded = Normalize(rule.Path);
            if (candidate.Equals(excluded, StringComparison.OrdinalIgnoreCase)) return true;
            if (!rule.IsFolder) continue;
            var prefix = System.IO.Path.EndsInDirectorySeparator(excluded) ? excluded : excluded + System.IO.Path.DirectorySeparatorChar;
            if (candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    public static List<ExclusionRule> Validate(IEnumerable<ExclusionRule> rules) =>
        rules.Select(r => new ExclusionRule(Normalize(r.Path), r.IsFolder))
            .DistinctBy(r => (r.Path.ToUpperInvariant(), r.IsFolder)).ToList();
}

public static class RecyclePolicy
{
    public static List<int> FullySelectedGroups(IEnumerable<DuplicateItem> items, IEnumerable<DuplicateItem> chosen)
    {
        var paths = chosen.Select(x => x.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return items.GroupBy(x => x.Group).Where(g => g.All(x => paths.Contains(x.Path))).Select(g => g.Key).ToList();
    }
}
