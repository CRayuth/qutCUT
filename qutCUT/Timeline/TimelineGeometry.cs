using qutCUT.Utilities;

namespace qutCUT.Timeline;

public sealed class TimelineGeometry(double pixelsPerFrame, double headerHeight, double trackHeight)
{
    public double PixelsPerFrame { get; set; } = pixelsPerFrame;
    public double HeaderHeight   { get; } = headerHeight;
    public double TrackHeight    { get; } = trackHeight;

    public double FrameToX(long frame)  => frame * PixelsPerFrame;
    public long   XToFrame(double x)    => (long)(x / PixelsPerFrame);
    public double DurationToWidth(long frames) => frames * PixelsPerFrame;
    public long   WidthToFrames(double width)  => (long)(width / PixelsPerFrame);

    public double TrackToY(int trackIndex)   => HeaderHeight + trackIndex * TrackHeight;
    public int    YToTrackIndex(double y)    => Math.Max(0, (int)((y - HeaderHeight) / TrackHeight));

    public (double x, double width) ClipRect(long startFrame, long durationFrames) =>
        (FrameToX(startFrame), DurationToWidth(durationFrames));
}
