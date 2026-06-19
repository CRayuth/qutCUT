using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace qutCUT.UI;

// Windows equivalent of AppTheme.swift — all UI constants live here.
// Never hardcode numeric values in XAML or code-behind; always use these.
public static class AppTheme
{
    public static class Spacing
    {
        public const double Xxs = 2;
        public const double Xs  = 4;
        public const double Sm  = 6;
        public const double Md  = 8;
        public const double Lg  = 12;
        public const double Xl  = 16;
        public const double Xxl = 24;
        public const double Xxxl = 32;
    }

    public static class FontSize
    {
        public const double Xxs     = 9;
        public const double Xs      = 10;
        public const double Sm      = 11;
        public const double Md      = 12;
        public const double Lg      = 13;
        public const double Xl      = 14;
        public const double Xxl     = 16;
        public const double Display  = 20;
        public const double Hero     = 28;
    }

    public static class Radius
    {
        public const double Xs  = 3;
        public const double Sm  = 5;
        public const double Md  = 7;
        public const double Lg  = 10;
        public const double Xl  = 14;
        public const double Full = 999;
    }

    public static class BorderWidth
    {
        public const double Hairline = 0.5;
        public const double Thin     = 1;
        public const double Medium   = 1.5;
        public const double Thick    = 2;
    }

    public static class Opacity
    {
        public const double Subtle     = 0.04;
        public const double Faint      = 0.08;
        public const double Muted      = 0.16;
        public const double Medium     = 0.32;
        public const double Strong     = 0.56;
        public const double Prominent  = 0.80;
    }

    public static class IconSize
    {
        public const double Xs  = 10;
        public const double Sm  = 12;
        public const double Md  = 14;
        public const double Lg  = 16;
        public const double Xl  = 20;
        public const double Xxl = 24;
    }

    public static class Anim
    {
        public const int Fast     = 120;  // ms
        public const int Normal   = 200;
        public const int Slow     = 320;
        public const int VerySlow = 500;
    }

    // Colors — dark theme focused (professional video editor)
    public static class Background
    {
        public static readonly Color Primary    = Color.FromArgb(255, 18,  18,  20);
        public static readonly Color Secondary  = Color.FromArgb(255, 26,  26,  30);
        public static readonly Color Tertiary   = Color.FromArgb(255, 36,  36,  42);
        public static readonly Color Elevated   = Color.FromArgb(255, 46,  46,  54);
        public static readonly Color Overlay    = Color.FromArgb(200, 12,  12,  14);
        public static readonly Color Accent     = Color.FromArgb(255, 99,  102, 241);
        public static readonly Color AccentMuted = Color.FromArgb(40,  99,  102, 241);
    }

    public static class Text
    {
        public static readonly Color Primary    = Color.FromArgb(255, 248, 248, 252);
        public static readonly Color Secondary  = Color.FromArgb(200, 180, 180, 196);
        public static readonly Color Tertiary   = Color.FromArgb(150, 130, 130, 148);
        public static readonly Color Disabled   = Color.FromArgb(80,  120, 120, 136);
        public static readonly Color Accent     = Color.FromArgb(255, 129, 132, 255);
        public static readonly Color Danger     = Color.FromArgb(255, 248, 113, 113);
    }

    public static class Border
    {
        public static readonly Color Subtle     = Color.FromArgb(30,  255, 255, 255);
        public static readonly Color Default    = Color.FromArgb(60,  255, 255, 255);
        public static readonly Color Strong     = Color.FromArgb(100, 255, 255, 255);
        public static readonly Color Accent     = Color.FromArgb(255, 99,  102, 241);
        public static readonly Color Danger     = Color.FromArgb(255, 239, 68,  68);
    }

    // Convenience brush factories
    public static SolidColorBrush Brush(Color c) => new(c);
}
