using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using qutCUT.Models;
using qutCUT.Utilities;
using Xabe.FFmpeg;

namespace qutCUT.Preview;

// Windows equivalent of VideoEngine.swift (was AVPlayer-based).
// Uses Windows.Media.Playback for realtime preview and FFmpeg for composition.
public sealed partial class VideoEngine : ObservableObject, IDisposable
{
    [ObservableProperty] private bool _isPlaying;
    [ObservableProperty] private long _currentFrame;
    [ObservableProperty] private double _volume = 1.0;
    [ObservableProperty] private bool _isMuted;

    public Timeline? Timeline { get; private set; }
    public int Fps => Timeline?.Fps ?? 30;

    private readonly DispatcherQueue _dispatcher;
    private readonly CompositionBuilder _builder;
    private System.Threading.Timer? _playTimer;
    private string? _composedVideoPath;
    private bool _needsRebuild = true;

    public event Action<long>? FrameChanged;

    public VideoEngine(DispatcherQueue dispatcher)
    {
        _dispatcher = dispatcher;
        _builder    = new CompositionBuilder();
    }

    public void LoadTimeline(Timeline timeline)
    {
        Timeline     = timeline;
        _needsRebuild = true;
        StopPlayback();
        SetFrame(0);
    }

    public void MarkDirty() => _needsRebuild = true;

    public void Play()
    {
        if (Timeline is null || IsPlaying) return;
        IsPlaying  = true;
        var interval = TimeSpan.FromSeconds(1.0 / Fps);
        _playTimer = new System.Threading.Timer(_ =>
        {
            _dispatcher.TryEnqueue(() =>
            {
                var next = CurrentFrame + 1;
                if (next >= Timeline.TotalFrames)
                {
                    StopPlayback();
                    return;
                }
                SetFrame(next);
            });
        }, null, interval, interval);
    }

    public void Pause() => StopPlayback();

    public void TogglePlayback()
    {
        if (IsPlaying) Pause();
        else Play();
    }

    public void SeekToFrame(long frame)
    {
        if (Timeline is null) return;
        SetFrame(Math.Clamp(frame, 0, Timeline.TotalFrames - 1));
    }

    public void SeekToSeconds(double seconds) =>
        SeekToFrame(TimeFormatting.SecondsToFrames(seconds, Fps));

    public void StepForward(int frames = 1) => SeekToFrame(CurrentFrame + frames);
    public void StepBack(int frames = 1)    => SeekToFrame(CurrentFrame - frames);

    // Builds the FFmpeg composition for the current timeline and returns the output path.
    // Used both for realtime preview and export.
    public async Task<string?> BuildCompositionAsync(
        string outputPath,
        ExportPreset preset,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        if (Timeline is null) return null;
        try
        {
            await _builder.BuildAsync(Timeline, outputPath, preset, progress, ct);
            _needsRebuild  = false;
            _composedVideoPath = outputPath;
            return outputPath;
        }
        catch (Exception ex)
        {
            Log.Preview.LogError(ex, "Composition build failed");
            return null;
        }
    }

    // Renders a single frame to a bitmap for the agent inspect_timeline tool.
    public async Task<byte[]?> RenderFrameAsync(long frame, CancellationToken ct = default)
    {
        if (Timeline is null) return null;
        var tempVideo = Path.GetTempFileName() + ".mp4";
        try
        {
            var singleFrameTimeline = CloneTimelineAtFrame(frame);
            await _builder.BuildAsync(singleFrameTimeline, tempVideo,
                new ExportPreset { Width = Timeline.Width, Height = Timeline.Height, Fps = 1 },
                null, ct);

            // Extract first frame as JPEG
            var frameFile = Path.GetTempFileName() + ".jpg";
            var conv = FFmpeg.Conversions.New()
                .AddParameter($"-i \"{tempVideo}\" -frames:v 1 \"{frameFile}\" -y");
            await conv.Start(ct);
            if (File.Exists(frameFile))
            {
                var bytes = await File.ReadAllBytesAsync(frameFile, ct);
                File.Delete(frameFile);
                return bytes;
            }
            return null;
        }
        finally
        {
            if (File.Exists(tempVideo)) File.Delete(tempVideo);
        }
    }

    private void SetFrame(long frame)
    {
        CurrentFrame = frame;
        FrameChanged?.Invoke(frame);
    }

    private void StopPlayback()
    {
        _playTimer?.Dispose();
        _playTimer = null;
        IsPlaying  = false;
    }

    private Timeline CloneTimelineAtFrame(long frame)
    {
        // Returns a 1-frame timeline snapshot at the given frame for rendering
        return new Timeline
        {
            Fps    = Timeline!.Fps,
            Width  = Timeline.Width,
            Height = Timeline.Height,
            Tracks = Timeline.Tracks.Select(t => new Track
            {
                Id    = t.Id,
                Type  = t.Type,
                Muted = t.Muted,
                Clips = t.Clips
                    .Where(c => c.StartFrame <= frame && c.StartFrame + c.DurationFrames > frame)
                    .Select(c => c with { StartFrame = 0, DurationFrames = 1 })
                    .ToList()
            }).ToList()
        };
    }

    public void Dispose()
    {
        StopPlayback();
        _builder.Dispose();
    }
}

public sealed class ExportPreset
{
    public int Width  { get; set; } = 1920;
    public int Height { get; set; } = 1080;
    public int Fps    { get; set; } = 30;
    public string VideoCodec { get; set; } = "libx264";
    public string AudioCodec { get; set; } = "aac";
    public string Crf        { get; set; } = "18";
    public string Preset     { get; set; } = "fast";

    public static ExportPreset H264_1080p => new();
    public static ExportPreset H265_1080p => new() { VideoCodec = "libx265", Crf = "20" };
    public static ExportPreset H264_4K    => new() { Width = 3840, Height = 2160 };
    public static ExportPreset ProRes     => new() { VideoCodec = "prores_ks", AudioCodec = "pcm_s16le" };
}
