using qutCUT.Models;
using qutCUT.Utilities;

namespace qutCUT.Timeline;

public sealed class SnapResult
{
    public long SnappedFrame { get; init; }
    public bool DidSnap      { get; init; }
    public long? SnapSourceFrame { get; init; }  // the frame we snapped to, for indicator rendering
}

public sealed class SnapEngine(double pixelsPerFrame)
{
    private double PixelsPerFrame { get; set; } = pixelsPerFrame;

    public SnapResult Snap(long candidateFrame, Timeline timeline, string? excludeClipId = null)
    {
        var snapPoints = CollectSnapPoints(timeline, excludeClipId);
        var best = FindClosest(candidateFrame, snapPoints);
        if (best is null) return new SnapResult { SnappedFrame = candidateFrame, DidSnap = false };

        var pixelDist = Math.Abs(best.Value - candidateFrame) * PixelsPerFrame;
        if (pixelDist > Constants.SnapThresholdPixels)
            return new SnapResult { SnappedFrame = candidateFrame, DidSnap = false };

        return new SnapResult { SnappedFrame = best.Value, DidSnap = true, SnapSourceFrame = best.Value };
    }

    private static IEnumerable<long> CollectSnapPoints(Timeline timeline, string? excludeId)
    {
        yield return 0; // timeline start
        yield return timeline.TotalFrames; // timeline end

        foreach (var track in timeline.Tracks)
        foreach (var clip in track.Clips)
        {
            if (clip.Id == excludeId) continue;
            yield return clip.StartFrame;
            yield return clip.StartFrame + clip.DurationFrames;
        }
    }

    private static long? FindClosest(long frame, IEnumerable<long> points)
    {
        long? best = null;
        long bestDist = long.MaxValue;
        foreach (var p in points)
        {
            var d = Math.Abs(p - frame);
            if (d < bestDist) { bestDist = d; best = p; }
        }
        return best;
    }

    public void UpdatePixelsPerFrame(double ppf) => PixelsPerFrame = ppf;
}
