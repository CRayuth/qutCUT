namespace qutCUT.Models;

public sealed class TextLayout
{
    // Normalized position (0-1 relative to canvas)
    public double X { get; set; } = 0.5;
    public double Y { get; set; } = 0.5;
    public double Width { get; set; } = 0.8;
    public double? Height { get; set; }

    public TextVerticalAlignment VerticalAlignment { get; set; } = TextVerticalAlignment.Center;
    public double PaddingX { get; set; }
    public double PaddingY { get; set; }
}

public enum TextVerticalAlignment { Top, Center, Bottom }
