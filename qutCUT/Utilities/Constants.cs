namespace qutCUT.Utilities;

public static class Constants
{
    // Timeline
    public const double DefaultPixelsPerFrame = 4.0;
    public const double MinPixelsPerFrame     = 0.25;
    public const double MaxPixelsPerFrame     = 64.0;
    public const double TrackHeight           = 52.0;
    public const double AudioTrackHeight      = 36.0;
    public const double TimelineHeaderHeight  = 28.0;

    // Snap
    public const double SnapThresholdPixels = 8.0;
    public const double PlayheadSnapRadius  = 12.0;

    // Zoom levels (pixels per frame)
    public static readonly double[] ZoomLevels =
    [
        0.25, 0.5, 1.0, 2.0, 4.0, 8.0, 16.0, 32.0, 64.0
    ];

    // Export
    public const int DefaultExportFps = 30;

    // Preview
    public const double CanvasDefaultZoom = 1.0;
    public const double CanvasMinZoom     = 0.1;
    public const double CanvasMaxZoom     = 4.0;

    // Generation
    public const int MaxGenerationPollingIntervalMs = 5000;
    public const int MinGenerationPollingIntervalMs = 1000;

    // Project
    public const string ProjectExtension   = ".qcut";
    public const string ProjectMediaFolder = "media";
    public const string TimelineFileName   = "project.json";
    public const string ManifestFileName   = "media.json";
    public const string GenLogFileName     = "generation-log.json";
    public const string ThumbnailFileName  = "thumbnail.jpg";
    public const string SessionsFolder     = "sessions";
}
