using System.Text.Json;

namespace DedupeLight;

sealed record AppSettings(string LastPath = "", decimal MinimumMegabytes = 1, bool IncludeHiddenSystem = false, int KeepRule = 0, List<ExclusionRule>? Exclusions = null)
{
    public static AppSettings Load(string path) { try { return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path)) ?? new(); } catch { return new(); } }
    public void Save(string path) { try { Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllText(path, JsonSerializer.Serialize(this)); } catch { } }
}
