using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using qutCUT.Account;
using qutCUT.Agent;
using qutCUT.Export;
using qutCUT.Generation;
using qutCUT.Models;
using qutCUT.Preview;
using qutCUT.Project;
using qutCUT.Utilities;

namespace qutCUT.Editor;

public sealed partial class EditorViewModel : ObservableObject, IDisposable
{
    // ── Core state ───────────────────────────────────────────────────────────
    public VideoProject Project { get; }
    [ObservableProperty] private Timeline _timeline;
    [ObservableProperty] private MediaManifest _manifest;

    // ── Playback ─────────────────────────────────────────────────────────────
    public VideoEngine VideoEngine { get; }
    [ObservableProperty] private long _currentFrame;
    [ObservableProperty] private bool _isPlaying;

    // ── Selection ─────────────────────────────────────────────────────────────
    [ObservableProperty] private List<string> _selectedClipIds = [];
    [ObservableProperty] private List<string> _selectedMediaAssetIds = [];

    // ── Media ─────────────────────────────────────────────────────────────────
    [ObservableProperty] private List<MediaAsset> _mediaAssets = [];

    // ── UI panels ─────────────────────────────────────────────────────────────
    [ObservableProperty] private bool _mediaPanelVisible = true;
    [ObservableProperty] private bool _inspectorVisible  = true;
    [ObservableProperty] private bool _agentPanelVisible;
    [ObservableProperty] private ActivePanel _focusedPanel = ActivePanel.Timeline;

    // ── Canvas ────────────────────────────────────────────────────────────────
    [ObservableProperty] private double _canvasZoom   = 1.0;
    [ObservableProperty] private double _canvasOffsetX;
    [ObservableProperty] private double _canvasOffsetY;

    // ── Timeline zoom ─────────────────────────────────────────────────────────
    [ObservableProperty] private double _pixelsPerFrame = Constants.DefaultPixelsPerFrame;

    // ── Tool mode ─────────────────────────────────────────────────────────────
    [ObservableProperty] private ToolMode _activeTool = ToolMode.Select;

    // ── Services ──────────────────────────────────────────────────────────────
    public AgentService   AgentService   { get; }
    public GenerationService GenerationService { get; }
    public ExportService  ExportService  { get; }

    public EditorViewModel(
        VideoProject project,
        AgentService agentService,
        GenerationService generationService,
        ExportService exportService,
        Microsoft.UI.Dispatching.DispatcherQueue dispatcher)
    {
        Project          = project;
        _timeline        = project.Timeline;
        _manifest        = project.Manifest;
        AgentService     = agentService;
        GenerationService = generationService;
        ExportService    = exportService;

        VideoEngine = new VideoEngine(dispatcher);
        VideoEngine.LoadTimeline(_timeline);
        VideoEngine.FrameChanged += f => CurrentFrame = f;

        GenerationService.JobCompleted += OnGenerationCompleted;
        GenerationService.JobFailed    += OnGenerationFailed;

        LoadMediaAssets();
    }

    // ── Timeline mutations ────────────────────────────────────────────────────

    public void AddClip(string trackId, Clip clip)
    {
        var track = Timeline.Tracks.FirstOrDefault(t => t.Id == trackId);
        if (track is null) return;
        track.Clips.Add(clip);
        OnTimelineChanged();
    }

    public void RemoveClip(string clipId)
    {
        foreach (var track in Timeline.Tracks)
            track.Clips.RemoveAll(c => c.Id == clipId);
        SelectedClipIds = SelectedClipIds.Where(id => id != clipId).ToList();
        OnTimelineChanged();
    }

    public void MoveClip(string clipId, string targetTrackId, long newStartFrame)
    {
        Clip? clip = null;
        foreach (var track in Timeline.Tracks)
        {
            var c = track.Clips.FirstOrDefault(c => c.Id == clipId);
            if (c is null) continue;
            clip = c;
            track.Clips.Remove(c);
            break;
        }
        if (clip is null) return;

        clip.StartFrame = newStartFrame;
        var target = Timeline.Tracks.FirstOrDefault(t => t.Id == targetTrackId);
        target?.Clips.Add(clip);
        OnTimelineChanged();
    }

    public void SplitClip(string clipId, long atFrame)
    {
        foreach (var track in Timeline.Tracks)
        {
            var clip = track.Clips.FirstOrDefault(c => c.Id == clipId);
            if (clip is null) continue;

            var splitOffset = atFrame - clip.StartFrame;
            if (splitOffset <= 0 || splitOffset >= clip.DurationFrames) return;

            var right = new Clip
            {
                MediaRef        = clip.MediaRef,
                MediaType       = clip.MediaType,
                StartFrame      = atFrame,
                DurationFrames  = clip.DurationFrames - splitOffset,
                TrimStartFrame  = clip.TrimStartFrame + splitOffset,
                TrimEndFrame    = clip.TrimEndFrame,
                Volume          = clip.Volume,
                Opacity         = clip.Opacity,
                Speed           = clip.Speed,
                Transform       = clip.Transform,
                Crop            = clip.Crop
            };

            clip.DurationFrames = splitOffset;
            clip.TrimEndFrame   = clip.TrimStartFrame + splitOffset;

            track.Clips.Add(right);
            OnTimelineChanged();
            return;
        }
    }

    public void AddTrack(TrackType type)
    {
        Timeline.Tracks.Add(new Track
        {
            Type = type,
            Name = type == TrackType.Video ? $"Video {Timeline.Tracks.Count(t => t.Type == TrackType.Video) + 1}"
                                           : $"Audio {Timeline.Tracks.Count(t => t.Type == TrackType.Audio) + 1}"
        });
        OnTimelineChanged();
    }

    // ── Clip property setters ─────────────────────────────────────────────────

    public void SetClipOpacity(string clipId, double opacity) =>
        MutateClip(clipId, c => c.Opacity = Math.Clamp(opacity, 0, 1));

    public void SetClipVolume(string clipId, double volume) =>
        MutateClip(clipId, c => c.Volume = Math.Max(0, volume));

    public void SetClipSpeed(string clipId, double speed) =>
        MutateClip(clipId, c => c.Speed = Math.Max(0.1, speed));

    public void SetClipTransform(string clipId, ClipTransform transform) =>
        MutateClip(clipId, c => c.Transform = transform);

    private void MutateClip(string clipId, Action<Clip> mutate)
    {
        var clip = FindClip(clipId);
        if (clip is null) return;
        mutate(clip);
        OnTimelineChanged();
    }

    // ── Media ─────────────────────────────────────────────────────────────────

    public async Task ImportMediaAsync(IEnumerable<string> filePaths)
    {
        foreach (var path in filePaths)
        {
            var localPath = Project.ImportMedia(path);
            var entry = await BuildManifestEntryAsync(localPath);
            Manifest.AddOrReplace(entry);
        }
        LoadMediaAssets();
        OnManifestChanged();
    }

    // ── Playback ──────────────────────────────────────────────────────────────

    [RelayCommand]
    public void TogglePlayback() => VideoEngine.TogglePlayback();

    [RelayCommand]
    public void SeekToStart() => VideoEngine.SeekToFrame(0);

    [RelayCommand]
    public void SeekToEnd() => VideoEngine.SeekToFrame(Timeline.TotalFrames - 1);

    // ── Persistence ───────────────────────────────────────────────────────────

    public void Save()
    {
        Project.Timeline = Timeline;
        Project.Manifest = Manifest;
        Project.Save();
    }

    // ── Generation callbacks ──────────────────────────────────────────────────

    private void OnGenerationCompleted(string assetId, string localPath)
    {
        var entry = Manifest.Find(assetId);
        if (entry is null) return;
        entry.Source = MediaResolver.ProjectRelativeSource(
            Path.GetRelativePath(Project.MediaDirectory, localPath));
        LoadMediaAssets();
        OnManifestChanged();
        Log.Generation.LogInformation("Asset ready: {id}", assetId);
    }

    private void OnGenerationFailed(string assetId, string error) =>
        Log.Generation.LogError("Asset failed: {id} — {err}", assetId, error);

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Clip? FindClip(string id)
    {
        foreach (var track in Timeline.Tracks)
        {
            var c = track.Clips.FirstOrDefault(c => c.Id == id);
            if (c is not null) return c;
        }
        return null;
    }

    private void LoadMediaAssets()
    {
        var resolver = Project.CreateResolver();
        MediaAssets = Manifest.Entries.Select(e => new MediaAsset
        {
            Id         = e.Id,
            Name       = e.Name,
            Type       = e.Type,
            LocalPath  = resolver.Resolve(e),
            RemoteUrl  = e.CachedRemoteUrl,
            Duration   = e.Duration,
            SourceWidth  = e.SourceWidth,
            SourceHeight = e.SourceHeight,
            SourceFps    = e.SourceFps,
            HasAudio     = e.HasAudio,
            FolderId     = e.FolderId,
            GenerationInput = e.GenerationInput
        }).ToList();
    }

    private static async Task<MediaManifestEntry> BuildManifestEntryAsync(string path)
    {
        var type = Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".mp4" or ".mov" or ".avi" or ".mkv" or ".webm" => ClipType.Video,
            ".mp3" or ".wav" or ".aac" or ".flac" or ".ogg" => ClipType.Audio,
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" => ClipType.Image,
            ".json" => ClipType.Lottie,
            _ => ClipType.Video
        };

        return new MediaManifestEntry
        {
            Name   = Path.GetFileNameWithoutExtension(path),
            Type   = type,
            Source = MediaResolver.ExternalSource(path)
        };
    }

    private void OnTimelineChanged()
    {
        Project.HasUnsavedChanges = true;
        VideoEngine.MarkDirty();
        OnPropertyChanged(nameof(Timeline));
    }

    private void OnManifestChanged()
    {
        Project.HasUnsavedChanges = true;
        OnPropertyChanged(nameof(Manifest));
    }

    public void Dispose()
    {
        GenerationService.JobCompleted -= OnGenerationCompleted;
        GenerationService.JobFailed    -= OnGenerationFailed;
        VideoEngine.Dispose();
    }
}

public enum ActivePanel { Timeline, MediaPanel, Inspector, Agent, Preview }
public enum ToolMode { Select, Text, Razor, Hand }
