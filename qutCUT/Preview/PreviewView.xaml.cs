using Microsoft.UI.Xaml.Controls;
using Windows.Media.Core;
using Windows.Media.Playback;
using qutCUT.Editor;

namespace qutCUT.Preview;

public sealed partial class PreviewView : UserControl
{
    private EditorViewModel? _viewModel;
    private MediaPlayer? _player;
    private bool _isBuildingComposition;
    private string? _composedPath;

    public bool IsBuildingComposition => _isBuildingComposition;

    public EditorViewModel? ViewModel
    {
        get => _viewModel;
        set
        {
            if (_viewModel == value) return;
            Unsubscribe();
            _viewModel = value;
            Subscribe();
        }
    }

    public PreviewView()
    {
        InitializeComponent();
        _player = new MediaPlayer();
        Player.SetMediaPlayer(_player);
    }

    private void Subscribe()
    {
        if (_viewModel is null) return;
        _viewModel.VideoEngine.FrameChanged += OnFrameChanged;
        _viewModel.PropertyChanged += OnViewModelChanged;
    }

    private void Unsubscribe()
    {
        if (_viewModel is null) return;
        _viewModel.VideoEngine.FrameChanged -= OnFrameChanged;
        _viewModel.PropertyChanged -= OnViewModelChanged;
    }

    private void OnViewModelChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EditorViewModel.Timeline))
            _ = RebuildAsync();
    }

    private void OnFrameChanged(long frame)
    {
        if (_player is null || _composedPath is null) return;
        var pos = TimeSpan.FromSeconds(frame / (double)(_viewModel?.Timeline.Fps ?? 30));
        _player.PlaybackSession.Position = pos;
    }

    private async Task RebuildAsync()
    {
        if (_viewModel is null) return;
        _isBuildingComposition = true;
        Bindings.Update();

        var outPath = Path.Combine(Path.GetTempPath(), "qutCUT_preview.mp4");
        var result  = await _viewModel.VideoEngine.BuildCompositionAsync(outPath, ExportPreset.H264_1080p);

        if (result is not null)
        {
            _composedPath = result;
            var source = MediaSource.CreateFromUri(new Uri(result));
            _player!.Source = source;
            _player.PlaybackSession.Position = TimeSpan.Zero;
        }

        _isBuildingComposition = false;
        Bindings.Update();
    }
}
