using System.Text.Json.Serialization;

namespace qutCUT.Models;

public sealed class Timeline
{
    public int Fps { get; set; } = 30;
    public int Width { get; set; } = 1920;
    public int Height { get; set; } = 1080;
    public List<Track> Tracks { get; set; } = [];

    [JsonIgnore]
    public long TotalFrames => Tracks
        .SelectMany(t => t.Clips)
        .Select(c => c.StartFrame + c.DurationFrames)
        .DefaultIfEmpty(0)
        .Max();

    [JsonIgnore]
    public double TotalSeconds => TotalFrames / (double)Fps;
}

public sealed class Track
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public TrackType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Muted { get; set; }
    public bool Hidden { get; set; }
    public bool SyncLocked { get; set; }
    public double Volume { get; set; } = 1.0;
    public List<Clip> Clips { get; set; } = [];
}

public enum TrackType { Video, Audio }

public sealed class Clip
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string? MediaRef { get; set; }
    public ClipType MediaType { get; set; }
    public SourceClipType SourceClipType { get; set; }

    // Timeline placement
    public long StartFrame { get; set; }
    public long DurationFrames { get; set; }

    // Source trim (frames within the source asset)
    public long TrimStartFrame { get; set; }
    public long TrimEndFrame { get; set; }

    // Playback
    public double Speed { get; set; } = 1.0;
    public double Volume { get; set; } = 1.0;
    public double Opacity { get; set; } = 1.0;

    // Fades (in frames)
    public long FadeInFrames { get; set; }
    public long FadeOutFrames { get; set; }

    // Spatial transform
    public ClipTransform Transform { get; set; } = new();
    public ClipCrop Crop { get; set; } = new();

    // Keyframe animation
    public List<Keyframe> Keyframes { get; set; } = [];

    // Text (text clips only)
    public string? TextContent { get; set; }
    public TextStyle? TextStyle { get; set; }
    public TextLayout? TextLayout { get; set; }

    // Grouping
    public string? LinkGroupId { get; set; }
    public string? CaptionGroupId { get; set; }
}

public sealed class ClipTransform
{
    public double X { get; set; }
    public double Y { get; set; }
    public double ScaleX { get; set; } = 1.0;
    public double ScaleY { get; set; } = 1.0;
    public double Rotation { get; set; }
    public double ShearX { get; set; }
    public double ShearY { get; set; }
}

public sealed class ClipCrop
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; } = 1.0;
    public double Height { get; set; } = 1.0;
}
