using System.Text.Json.Serialization;

namespace qutCUT.Models;

public sealed class MediaAsset
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public ClipType Type { get; set; }

    // Resolved local file path
    public string? LocalPath { get; set; }

    // Remote URL (for generated/cloud assets)
    public string? RemoteUrl { get; set; }
    public DateTime? RemoteUrlExpiresAt { get; set; }

    // Metadata
    public TimeSpan Duration { get; set; }
    public int SourceWidth { get; set; }
    public int SourceHeight { get; set; }
    public double SourceFps { get; set; }
    public bool HasAudio { get; set; }
    public long FileSizeBytes { get; set; }

    // Library organization
    public string? FolderId { get; set; }
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;

    // Generation metadata (if this asset was AI-generated)
    public GenerationInput? GenerationInput { get; set; }
    public GenerationStatus GenerationStatus { get; set; } = GenerationStatus.Ready;
    public double GenerationProgress { get; set; }
    public string? GenerationJobId { get; set; }
    public string? GenerationError { get; set; }

    [JsonIgnore]
    public bool IsGenerating => GenerationStatus == GenerationStatus.Pending
                             || GenerationStatus == GenerationStatus.Processing;

    [JsonIgnore]
    public bool IsReady => GenerationStatus == GenerationStatus.Ready
                        && (LocalPath != null || RemoteUrl != null);

    [JsonIgnore]
    public string? EffectiveUrl => LocalPath ?? RemoteUrl;
}

public sealed class GenerationInput
{
    public string Prompt { get; set; } = string.Empty;
    public string? NegativePrompt { get; set; }
    public string? Model { get; set; }
    public double? DurationSeconds { get; set; }
    public string? AspectRatio { get; set; }
    public string? ReferenceMediaId { get; set; }
    public Dictionary<string, object> ExtraParams { get; set; } = [];
}

public enum GenerationStatus
{
    Ready,
    Pending,
    Processing,
    Failed
}
