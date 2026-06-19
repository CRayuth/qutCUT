using System.Text.Json;

namespace qutCUT.Models;

public sealed class MediaManifest
{
    public int Version { get; set; } = 1;
    public List<MediaManifestEntry> Entries { get; set; } = [];
    public List<MediaFolder> Folders { get; set; } = [];

    public MediaManifestEntry? Find(string id) =>
        Entries.FirstOrDefault(e => e.Id == id);

    public void AddOrReplace(MediaManifestEntry entry)
    {
        var idx = Entries.FindIndex(e => e.Id == entry.Id);
        if (idx >= 0) Entries[idx] = entry;
        else Entries.Add(entry);
    }

    public bool Remove(string id) =>
        Entries.RemoveAll(e => e.Id == id) > 0;

    public static MediaManifest Empty() => new();

    public string ToJson() =>
        JsonSerializer.Serialize(this, JsonOptions.Default);

    public static MediaManifest FromJson(string json) =>
        JsonSerializer.Deserialize<MediaManifest>(json, JsonOptions.Default) ?? Empty();
}

public sealed class MediaManifestEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public ClipType Type { get; set; }

    // "external:<absolute-path>" or "project:<relative-path>"
    public string Source { get; set; } = string.Empty;

    // Cached metadata
    public TimeSpan Duration { get; set; }
    public int SourceWidth { get; set; }
    public int SourceHeight { get; set; }
    public double SourceFps { get; set; }
    public bool HasAudio { get; set; }

    // Library organization
    public string? FolderId { get; set; }
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;

    // Generation metadata
    public GenerationInput? GenerationInput { get; set; }
    public string? CachedRemoteUrl { get; set; }
    public DateTime? CachedRemoteUrlExpiresAt { get; set; }
    public string? GenerationJobId { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsExternal => Source.StartsWith("external:", StringComparison.Ordinal);

    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsProjectRelative => Source.StartsWith("project:", StringComparison.Ordinal);

    [System.Text.Json.Serialization.JsonIgnore]
    public string RawPath => IsExternal
        ? Source["external:".Length..]
        : Source["project:".Length..];
}

internal static class JsonOptions
{
    internal static readonly JsonSerializerOptions Default = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };
}
