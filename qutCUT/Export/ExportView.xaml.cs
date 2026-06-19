using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using qutCUT.Editor;

namespace qutCUT.Export;

public sealed partial class ExportView : ContentDialog
{
    private readonly EditorViewModel _viewModel;

    public bool IsExporting        => _viewModel.ExportService.IsExporting;
    public double ExportProgressValue => _viewModel.ExportService.Progress * 100;
    public string StatusMessage    => _viewModel.ExportService.StatusMessage;

    public ExportView(EditorViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        PrimaryButtonClick += OnExportClick;

        _viewModel.ExportService.PropertyChanged += (_, _) => Bindings.Update();
    }

    private async void OnBrowse(object sender, RoutedEventArgs e)
    {
        var picker = new FileSavePicker { SuggestedFileName = "export" };
        picker.FileTypeChoices.Add("Video", [".mp4", ".mov"]);
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSaveFileAsync();
        if (file is not null) OutputPath.Text = file.Path;
    }

    private async void OnExportClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var deferral = args.GetDeferral();
        try
        {
            if (string.IsNullOrEmpty(OutputPath.Text)) return;

            var opts = new ExportOptions
            {
                Format     = FormatPicker.SelectedIndex switch { 1 => ExportFormat.H265, 2 => ExportFormat.ProRes, _ => ExportFormat.H264 },
                Resolution = ResolutionPicker.SelectedIndex switch { 0 => ExportResolution.R720p, 2 => ExportResolution.R4K, _ => ExportResolution.R1080p },
                OutputPath = OutputPath.Text
            };

            await _viewModel.ExportService.ExportAsync(_viewModel.Timeline, opts);
        }
        finally
        {
            deferral.Complete();
        }
    }
}
