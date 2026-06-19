using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;

namespace qutCUT.Project;

public sealed partial class HomeView : UserControl
{
    public IReadOnlyList<RecentProject> Recent =>
        App.State.Registry.RecentProjects;

    public HomeView()
    {
        InitializeComponent();
    }

    private async void OnNewProject(object sender, RoutedEventArgs e)
    {
        var picker = new FileSavePicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = "Untitled Project"
        };
        picker.FileTypeChoices.Add("qutCUT Project", [".qcut"]);

        // Associate with window (WinUI 3 requirement)
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.Current.As<App>()._window!);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSaveFileAsync();
        if (file is null) return;

        App.State.CreateProject(file.Path);
    }

    private async void OnOpenProject(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary
        };
        picker.FileTypeFilter.Add(".qcut");

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.Current.As<App>()._window!);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSingleFileAsync();
        if (file is null) return;

        App.State.OpenProject(file.Path);
    }

    private void OnOpenRecent(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string path)
            App.State.OpenProject(path);
    }
}
