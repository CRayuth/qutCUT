namespace qutCUT.Models;

public sealed class Keyframe
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Property { get; set; } = string.Empty;
    public long Frame { get; set; }
    public double Value { get; set; }
    public KeyframeInterpolation Interpolation { get; set; } = KeyframeInterpolation.Linear;

    // Optional bezier control points for Bezier interpolation
    public double? InTangent { get; set; }
    public double? OutTangent { get; set; }
}

public enum KeyframeInterpolation
{
    Linear,
    Hold,
    Bezier,
    EaseIn,
    EaseOut,
    EaseInOut
}

public static class KeyframeProperty
{
    public const string Opacity = "opacity";
    public const string Volume = "volume";
    public const string PositionX = "transform.x";
    public const string PositionY = "transform.y";
    public const string ScaleX = "transform.scaleX";
    public const string ScaleY = "transform.scaleY";
    public const string Rotation = "transform.rotation";
    public const string CropX = "crop.x";
    public const string CropY = "crop.y";
    public const string CropWidth = "crop.width";
    public const string CropHeight = "crop.height";
}
