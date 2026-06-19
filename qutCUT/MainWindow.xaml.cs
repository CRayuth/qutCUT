using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace qutCUT;

public sealed partial class MainWindow : Window
{
    public Microsoft.UI.Xaml.Visibility ShowHome   => App.State.ActiveEditor is null
        ? Visibility.Visible : Visibility.Collapsed;
    public Microsoft.UI.Xaml.Visibility ShowEditor => App.State.ActiveEditor is not null
        ? Visibility.Visible : Visibility.Collapsed;

    public MainWindow()
    {
        InitializeComponent();
        ConfigureWindow();
        App.State.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(AppState.ActiveEditor))
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    OnPropertyChanged(nameof(ShowHome));
                    OnPropertyChanged(nameof(ShowEditor));
                    if (App.State.ActiveEditor is not null)
                        EditorView.ViewModel = App.State.ActiveEditor;
                });
            }
        };
    }

    private void ConfigureWindow()
    {
        Title = "qutCUT";

        var appWindow = AppWindow;
        appWindow.Resize(new SizeInt32(1600, 960));
        appWindow.SetPresenter(AppWindowPresenterKind.Default);

        // Custom title bar (mica material)
        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            var tb = appWindow.TitleBar;
            tb.ExtendsContentIntoTitleBar  = true;
            tb.ButtonBackgroundColor       = Colors.Transparent;
            tb.ButtonInactiveBackgroundColor = Colors.Transparent;
        }

        // Center on screen
        var display = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
        var x = (display.WorkArea.Width  - 1600) / 2;
        var y = (display.WorkArea.Height - 960)  / 2;
        appWindow.Move(new PointInt32(x, y));
    }

    private void OnPropertyChanged(string name) =>
        DispatcherQueue.TryEnqueue(() => Bindings.Update());
}
