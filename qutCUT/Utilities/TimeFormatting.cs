namespace qutCUT.Utilities;

public static class TimeFormatting
{
    // Converts frame count to SMPTE timecode HH:MM:SS:FF
    public static string ToTimecode(long frame, int fps)
    {
        var totalSeconds = frame / fps;
        var frames       = frame % fps;
        var seconds      = totalSeconds % 60;
        var minutes      = (totalSeconds / 60) % 60;
        var hours        = totalSeconds / 3600;
        return $"{hours:D2}:{minutes:D2}:{seconds:D2}:{frames:D2}";
    }

    public static string ToShortTimecode(long frame, int fps)
    {
        var totalSeconds = frame / fps;
        var frames       = frame % fps;
        var seconds      = totalSeconds % 60;
        var minutes      = (totalSeconds / 60) % 60;
        return minutes > 0
            ? $"{minutes:D2}:{seconds:D2}:{frames:D2}"
            : $"{seconds:D2}:{frames:D2}";
    }

    public static string FormatSeconds(double seconds)
    {
        var ts = TimeSpan.FromSeconds(seconds);
        return ts.Hours > 0
            ? $"{ts.Hours:D1}:{ts.Minutes:D2}:{ts.Seconds:D2}"
            : $"{ts.Minutes:D1}:{ts.Seconds:D2}";
    }

    public static long SecondsToFrames(double seconds, int fps) =>
        (long)(seconds * fps);

    public static double FramesToSeconds(long frames, int fps) =>
        frames / (double)fps;
}
