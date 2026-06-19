using Microsoft.UI.Xaml;
using qutCUT.Utilities;
using Sentry;
using Velopack;

namespace qutCUT;

public partial class App : Application
{
    public static AppState State { get; private set; } = null!;
    private Window? _window;

    public App()
    {
        InitializeComponent();
        RequestedTheme = ApplicationTheme.Dark;
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        BootstrapServices();

        _window = new MainWindow();
        _window.Activate();
    }

    private static void BootstrapServices()
    {
        // Logging
        var logFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(b =>
            b.AddDebug().SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug));
        Log.Bootstrap(logFactory);

        // Crash reporting (Sentry — equivalent of Sentry-cocoa)
        SentrySdk.Init(o =>
        {
            o.Dsn = "https://YOUR_SENTRY_DSN@sentry.io/PROJECT_ID";
            o.Debug = false;
            o.TracesSampleRate = 0.1;
        });

        // Auto-updater (Velopack — equivalent of Sparkle)
        VelopackApp.Build().Run();

        // FFmpeg binary location
        Xabe.FFmpeg.FFmpeg.SetExecutablesPath(Path.Combine(AppContext.BaseDirectory, "ffmpeg"));

        Log.App.LogInformation("qutCUT starting");
    }
}

// Custom entry point — disables XAML-generated Main so we control startup order
internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        WinRT.ComWrappersSupport.InitializeComWrappers();
        Microsoft.UI.Xaml.Application.Start(p =>
        {
            var context = new Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext(
                Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
            System.Threading.SynchronizationContext.SetSynchronizationContext(context);

            AppState.State = new AppState(Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
            _ = new App();
        });
    }
}
