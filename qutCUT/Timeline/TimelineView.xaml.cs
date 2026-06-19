using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using qutCUT.Editor;
using qutCUT.Models;
using qutCUT.Utilities;
using Windows.Foundation;

namespace qutCUT.Timeline;

public sealed partial class TimelineView : UserControl
{
    private EditorViewModel? _viewModel;
    private TimelineGeometry _geo = new(Constants.DefaultPixelsPerFrame, Constants.TimelineHeaderHeight, Constants.TrackHeight);
    private SnapEngine _snap = new(Constants.DefaultPixelsPerFrame);

    // Drag state
    private string? _draggingClipId;
    private Point _dragStart;
    private long _dragOriginalFrame;

    public EditorViewModel? ViewModel
    {
        get => _viewModel;
        set
        {
            if (_viewModel == value) return;
            Unsubscribe();
            _viewModel = value;
            Subscribe();
            Render();
        }
    }

    public IReadOnlyList<Track> Tracks => _viewModel?.Timeline.Tracks ?? [];

    public TimelineView()
    {
        InitializeComponent();
    }

    private void Subscribe()
    {
        if (_viewModel is null) return;
        _viewModel.PropertyChanged += OnViewModelChanged;
        _viewModel.VideoEngine.FrameChanged += OnFrameChanged;
    }

    private void Unsubscribe()
    {
        if (_viewModel is null) return;
        _viewModel.PropertyChanged -= OnViewModelChanged;
        _viewModel.VideoEngine.FrameChanged -= OnFrameChanged;
    }

    private void OnViewModelChanged(object? s, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(EditorViewModel.Timeline) or nameof(EditorViewModel.PixelsPerFrame))
        {
            _geo.PixelsPerFrame = _viewModel!.PixelsPerFrame;
            _snap.UpdatePixelsPerFrame(_viewModel.PixelsPerFrame);
            Render();
            Bindings.Update(); // refresh track labels
        }
    }

    private void OnFrameChanged(long frame) => DrawPlayhead(frame);

    // ── Rendering ─────────────────────────────────────────────────────────────

    private void Render()
    {
        if (_viewModel is null) return;
        ClipCanvas.Children.Clear();

        var timeline   = _viewModel.Timeline;
        var totalWidth = _geo.FrameToX(timeline.TotalFrames + timeline.Fps * 5);
        ClipCanvas.Width  = Math.Max(totalWidth, ClipScroll.ActualWidth);
        ClipCanvas.Height = timeline.Tracks.Count * _geo.TrackHeight;

        DrawRuler();

        for (int ti = 0; ti < timeline.Tracks.Count; ti++)
        {
            var track = timeline.Tracks[ti];
            var y     = _geo.TrackToY(ti);

            // Track row background
            var rowBg = new Rectangle
            {
                Width  = ClipCanvas.Width,
                Height = _geo.TrackHeight,
                Fill   = new SolidColorBrush(ti % 2 == 0
                    ? Windows.UI.Color.FromArgb(255, 26, 26, 30)
                    : Windows.UI.Color.FromArgb(255, 30, 30, 36))
            };
            Canvas.SetTop(rowBg, y);
            ClipCanvas.Children.Add(rowBg);

            // Row divider
            var divider = new Line
            {
                X1 = 0, X2 = ClipCanvas.Width, Y1 = y + _geo.TrackHeight, Y2 = y + _geo.TrackHeight,
                Stroke = new SolidColorBrush(Windows.UI.Color.FromArgb(30, 255, 255, 255)),
                StrokeThickness = 0.5
            };
            ClipCanvas.Children.Add(divider);

            foreach (var clip in track.Clips)
                DrawClip(clip, y);
        }
    }

    private void DrawClip(Clip clip, double trackY)
    {
        var (x, width) = _geo.ClipRect(clip.StartFrame, clip.DurationFrames);
        if (width < 1) return;

        var isSelected = _viewModel?.SelectedClipIds.Contains(clip.Id) == true;

        var clipColor = clip.MediaType switch
        {
            ClipType.Video => Windows.UI.Color.FromArgb(255, 59,  130, 246),
            ClipType.Audio => Windows.UI.Color.FromArgb(255, 16,  185, 129),
            ClipType.Image => Windows.UI.Color.FromArgb(255, 139, 92,  246),
            ClipType.Text  => Windows.UI.Color.FromArgb(255, 245, 158, 11),
            _              => Windows.UI.Color.FromArgb(255, 99,  102, 241)
        };

        var rect = new Rectangle
        {
            Width   = Math.Max(width - 2, 1),
            Height  = _geo.TrackHeight - 4,
            Fill    = new SolidColorBrush(clipColor),
            RadiusX = 4, RadiusY = 4,
            Opacity = isSelected ? 1.0 : 0.85,
            Stroke  = isSelected ? new SolidColorBrush(Colors.White) : null,
            StrokeThickness = isSelected ? 1.5 : 0
        };
        rect.Tag = clip.Id;
        Canvas.SetLeft(rect, x + 1);
        Canvas.SetTop(rect, trackY + 2);
        ClipCanvas.Children.Add(rect);

        // Clip name label
        if (width > 30)
        {
            var name = clip.MediaRef is not null
                ? System.IO.Path.GetFileNameWithoutExtension(clip.MediaRef)
                : (clip.TextContent ?? "Text");

            var label = new TextBlock
            {
                Text         = name,
                FontSize     = 10,
                Foreground   = new SolidColorBrush(Colors.White),
                TextTrimming = TextTrimming.CharacterEllipsis,
                Width        = width - 8
            };
            Canvas.SetLeft(label, x + 5);
            Canvas.SetTop(label, trackY + 6);
            ClipCanvas.Children.Add(label);
        }
    }

    private void DrawRuler()
    {
        RulerCanvas.Children.Clear();
        if (_viewModel is null) return;

        var fps   = _viewModel.Timeline.Fps;
        var width = ClipCanvas.Width;

        // Draw major marks every second, minor every 5 frames
        for (long f = 0; f * _geo.PixelsPerFrame < width; f += fps)
        {
            var x    = _geo.FrameToX(f);
            var line = new Line
            {
                X1 = x, X2 = x, Y1 = 16, Y2 = 28,
                Stroke = new SolidColorBrush(Windows.UI.Color.FromArgb(60, 255, 255, 255)),
                StrokeThickness = 0.5
            };
            RulerCanvas.Children.Add(line);

            var label = new TextBlock
            {
                Text     = TimeFormatting.ToShortTimecode(f, fps),
                FontSize = 9,
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(150, 200, 200, 210))
            };
            Canvas.SetLeft(label, x + 2);
            Canvas.SetTop(label, 2);
            RulerCanvas.Children.Add(label);
        }
    }

    private void DrawPlayhead(long frame)
    {
        PlayheadCanvas.Children.Clear();
        var x    = _geo.FrameToX(frame);
        var line = new Line
        {
            X1 = x, X2 = x, Y1 = 0, Y2 = PlayheadCanvas.ActualHeight,
            Stroke = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 239, 68, 68)),
            StrokeThickness = 1.5
        };
        PlayheadCanvas.Children.Add(line);

        // Triangle head
        var head = new Polygon
        {
            Points = new PointCollection { new(x - 5, 0), new(x + 5, 0), new(x, 10) },
            Fill   = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 239, 68, 68))
        };
        PlayheadCanvas.Children.Add(head);
    }

    // ── Input ─────────────────────────────────────────────────────────────────

    private void OnRulerScrub(object sender, PointerRoutedEventArgs e)
    {
        if (_viewModel is null) return;
        var x     = e.GetCurrentPoint(RulerCanvas).Position.X;
        var frame = _geo.XToFrame(x);
        _viewModel.VideoEngine.SeekToFrame(frame);
    }

    private void OnClipCanvasPress(object sender, PointerRoutedEventArgs e)
    {
        if (_viewModel is null) return;
        var pos = e.GetCurrentPoint(ClipCanvas).Position;

        // Hit test: find which clip was clicked
        foreach (var el in ClipCanvas.Children.OfType<Rectangle>())
        {
            if (el.Tag is not string clipId) continue;
            var left = Canvas.GetLeft(el);
            var top  = Canvas.GetTop(el);
            if (pos.X >= left && pos.X <= left + el.Width &&
                pos.Y >= top  && pos.Y <= top  + el.Height)
            {
                _viewModel.SelectedClipIds = [clipId];
                _draggingClipId    = clipId;
                _dragStart         = pos;
                ClipCanvas.CapturePointer(e.Pointer);
                Render();
                return;
            }
        }

        // Clicked empty space — deselect and scrub
        _viewModel.SelectedClipIds = [];
        var frame = _geo.XToFrame(pos.X);
        _viewModel.VideoEngine.SeekToFrame(frame);
        Render();
    }

    private void OnClipCanvasMove(object sender, PointerRoutedEventArgs e)
    {
        if (_draggingClipId is null || _viewModel is null) return;
        var pos    = e.GetCurrentPoint(ClipCanvas).Position;
        var deltaX = pos.X - _dragStart.X;
        var deltaF = _geo.WidthToFrames(deltaX);
        var newFrame = Math.Max(0, _dragOriginalFrame + deltaF);

        // Find track under cursor
        var trackIndex = _geo.YToTrackIndex(pos.Y);
        trackIndex = Math.Clamp(trackIndex, 0, _viewModel.Timeline.Tracks.Count - 1);
        var targetTrackId = _viewModel.Timeline.Tracks[trackIndex].Id;

        // Snap
        var snapResult = _snap.Snap(newFrame, _viewModel.Timeline, _draggingClipId);
        _viewModel.MoveClip(_draggingClipId, targetTrackId, snapResult.SnappedFrame);
        Render();
    }

    private void OnClipCanvasRelease(object sender, PointerRoutedEventArgs e)
    {
        _draggingClipId = null;
        ClipCanvas.ReleasePointerCaptures();
    }

    private async void OnClipDrop(object sender, DragEventArgs e)
    {
        if (_viewModel is null) return;
        var text = await e.DataView.GetTextAsync();
        if (!text.StartsWith("asset:")) return;

        var assetId = text["asset:".Length..];
        var asset   = _viewModel.MediaAssets.FirstOrDefault(a => a.Id == assetId);
        if (asset is null) return;

        var pos        = e.GetPosition(ClipCanvas);
        var startFrame = Math.Max(0, _geo.XToFrame(pos.X));
        var trackIndex = Math.Clamp(_geo.YToTrackIndex(pos.Y), 0, _viewModel.Timeline.Tracks.Count - 1);
        var trackId    = _viewModel.Timeline.Tracks[trackIndex].Id;

        var durationFrames = asset.Duration.TotalSeconds > 0
            ? (long)(asset.Duration.TotalSeconds * _viewModel.Timeline.Fps)
            : _viewModel.Timeline.Fps * 5;

        var clip = new Clip
        {
            MediaRef       = asset.LocalPath ?? asset.RemoteUrl,
            MediaType      = asset.Type,
            StartFrame     = startFrame,
            DurationFrames = durationFrames,
            TrimEndFrame   = durationFrames
        };

        _viewModel.AddClip(trackId, clip);
        Render();
    }

    private void OnClipDragOver(object sender, DragEventArgs e) =>
        e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;

    private void OnAddTrack(object sender, RoutedEventArgs e) =>
        _viewModel?.AddTrack(TrackType.Video);
}
