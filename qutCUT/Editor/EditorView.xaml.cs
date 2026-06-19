using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using qutCUT.Utilities;

namespace qutCUT.Editor;

public sealed partial class EditorView : UserControl
{
    private EditorViewModel? _viewModel;
    public EditorViewModel? ViewModel
    {
        get => _viewModel;
        set
        {
            if (_viewModel == value) return;
            _viewModel = value;
            Bindings.Update();
        }
    }

    public string Timecode => ViewModel is null ? "00:00:00"
        : TimeFormatting.ToTimecode(ViewModel.CurrentFrame, ViewModel.Timeline.Fps);

    public string PlayButtonLabel => ViewModel?.IsPlaying == true ? "⏸" : "▶";

    public bool IsSelectTool => ViewModel?.ActiveTool == ToolMode.Select;
    public bool IsRazorTool  => ViewModel?.ActiveTool == ToolMode.Razor;
    public bool IsTextTool   => ViewModel?.ActiveTool == ToolMode.Text;

    public GridLength MediaPanelWidth  => ViewModel?.MediaPanelVisible == true  ? new GridLength(280) : GridLength.Auto;
    public GridLength InspectorWidth   => ViewModel?.InspectorVisible == true   ? new GridLength(260) : GridLength.Auto;
    public GridLength AgentPanelWidth  => ViewModel?.AgentPanelVisible == true  ? new GridLength(340) : GridLength.Auto;

    public Visibility MediaPanelVisibility => ViewModel?.MediaPanelVisible == true  ? Visibility.Visible : Visibility.Collapsed;
    public Visibility InspectorVisibility  => ViewModel?.InspectorVisible == true   ? Visibility.Visible : Visibility.Collapsed;
    public Visibility AgentPanelVisibility => ViewModel?.AgentPanelVisible == true  ? Visibility.Visible : Visibility.Collapsed;

    public EditorView()
    {
        InitializeComponent();
    }

    private void OnTogglePlay(object s, RoutedEventArgs e) => ViewModel?.TogglePlayback();
    private void OnSeekStart(object s, RoutedEventArgs e)  => ViewModel?.SeekToStart();
    private void OnSeekEnd(object s, RoutedEventArgs e)    => ViewModel?.SeekToEnd();
    private void OnStepBack(object s, RoutedEventArgs e)   => ViewModel?.VideoEngine.StepBack();
    private void OnStepForward(object s, RoutedEventArgs e) => ViewModel?.VideoEngine.StepForward();

    private void OnSelectTool(object s, RoutedEventArgs e) => SetTool(ToolMode.Select);
    private void OnRazorTool(object s, RoutedEventArgs e)  => SetTool(ToolMode.Razor);
    private void OnTextTool(object s, RoutedEventArgs e)   => SetTool(ToolMode.Text);
    private void SetTool(ToolMode mode) { if (ViewModel is not null) ViewModel.ActiveTool = mode; }

    private async void OnExport(object s, RoutedEventArgs e)
    {
        if (ViewModel is null) return;
        var dialog = new Export.ExportView(ViewModel);
        dialog.XamlRoot = XamlRoot;
        await dialog.ShowAsync();
    }
}
