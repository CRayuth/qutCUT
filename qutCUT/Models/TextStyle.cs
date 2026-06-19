using System.Drawing;

namespace qutCUT.Models;

public sealed class TextStyle
{
    public string FontFamily { get; set; } = "Segoe UI";
    public double FontSize { get; set; } = 48;
    public FontWeightKind FontWeight { get; set; } = FontWeightKind.Regular;
    public bool Italic { get; set; }
    public string Color { get; set; } = "#FFFFFFFF";
    public string? BackgroundColor { get; set; }
    public TextAlignmentKind Alignment { get; set; } = TextAlignmentKind.Center;
    public double LineSpacing { get; set; } = 1.2;
    public double LetterSpacing { get; set; }
    public bool AllCaps { get; set; }
    public bool HasShadow { get; set; }
    public string ShadowColor { get; set; } = "#80000000";
    public double ShadowOffsetX { get; set; } = 2;
    public double ShadowOffsetY { get; set; } = 2;
    public double ShadowBlur { get; set; } = 4;
    public bool HasOutline { get; set; }
    public string OutlineColor { get; set; } = "#FF000000";
    public double OutlineWidth { get; set; } = 2;
}

public enum FontWeightKind { Thin, ExtraLight, Light, Regular, Medium, SemiBold, Bold, ExtraBold, Black }
public enum TextAlignmentKind { Left, Center, Right, Justified }
