using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using qutCUT.Editor;
using qutCUT.Models;

namespace qutCUT.Inspector;

public sealed partial class InspectorView : UserControl
{
    private EditorViewModel? _viewModel;
    public EditorViewModel? ViewModel
    {
        get => _viewModel;
        set
        {
            if (_viewModel == value) return;
            if (_viewModel != null) _viewModel.PropertyChanged -= OnViewModelChanged;
            _viewModel = value;
            if (_viewModel != null) _viewModel.PropertyChanged += OnViewModelChanged;
            RefreshFromSelection();
        }
    }

    public Visibility HasSelection => SelectedClip is not null ? Visibility.Visible : Visibility.Collapsed;

    private Clip? SelectedClip => _viewModel?.Timeline.Tracks
        .SelectMany(t => t.Clips)
        .FirstOrDefault(c => _viewModel.SelectedClipIds.Contains(c.Id));

    public InspectorView() => InitializeComponent();

    private void OnViewModelChanged(object? s, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(EditorViewModel.SelectedClipIds) or nameof(EditorViewModel.Timeline))
            RefreshFromSelection();
    }

    private void RefreshFromSelection()
    {
        var clip = SelectedClip;
        Bindings.Update();
        if (clip is null) return;

        TransformX.Value = clip.Transform.X;
        TransformY.Value = clip.Transform.Y;
        ScaleX.Value     = clip.Transform.ScaleX;
        ScaleY.Value     = clip.Transform.ScaleY;
        RotationBox.Value = clip.Transform.Rotation;
        OpacitySlider.Value = clip.Opacity;
        VolumeSlider.Value  = clip.Volume;
        SpeedBox.Value      = clip.Speed;
    }

    private void OnTransformXChanged(NumberBox s, NumberBoxValueChangedEventArgs e)   => ApplyTransform(x: e.NewValue);
    private void OnTransformYChanged(NumberBox s, NumberBoxValueChangedEventArgs e)   => ApplyTransform(y: e.NewValue);
    private void OnScaleXChanged(NumberBox s, NumberBoxValueChangedEventArgs e)       => ApplyTransform(sx: e.NewValue);
    private void OnScaleYChanged(NumberBox s, NumberBoxValueChangedEventArgs e)       => ApplyTransform(sy: e.NewValue);
    private void OnRotationChanged(NumberBox s, NumberBoxValueChangedEventArgs e)     => ApplyTransform(rot: e.NewValue);
    private void OnOpacityChanged(object s, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        => _viewModel?.SetClipOpacity(SelectedClip?.Id ?? "", e.NewValue);
    private void OnVolumeChanged(object s, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        => _viewModel?.SetClipVolume(SelectedClip?.Id ?? "", e.NewValue);
    private void OnSpeedChanged(NumberBox s, NumberBoxValueChangedEventArgs e)
        => _viewModel?.SetClipSpeed(SelectedClip?.Id ?? "", e.NewValue);

    private void ApplyTransform(double? x = null, double? y = null, double? sx = null, double? sy = null, double? rot = null)
    {
        var clip = SelectedClip;
        if (clip is null || _viewModel is null) return;
        var t = clip.Transform;
        _viewModel.SetClipTransform(clip.Id, new ClipTransform
        {
            X         = x   ?? t.X,
            Y         = y   ?? t.Y,
            ScaleX    = sx  ?? t.ScaleX,
            ScaleY    = sy  ?? t.ScaleY,
            Rotation  = rot ?? t.Rotation,
            ShearX    = t.ShearX,
            ShearY    = t.ShearY
        });
    }
}
