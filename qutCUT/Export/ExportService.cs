using CommunityToolkit.Mvvm.ComponentModel;
using qutCUT.Models;
using qutCUT.Preview;
using qutCUT.Utilities;

namespace qutCUT.Export;

public enum ExportFormat { H264, H265, ProRes }
public enum ExportResolution { R720p, R1080p, R4K, Custom }

public sealed class ExportOptions
{
    public ExportFormat Format         { get; set; } = ExportFormat.H264;
    public ExportResolution Resolution { get; set; } = ExportResolution.R1080p;
    public int CustomWidth             { get; set; }
    public int CustomHeight            { get; set; }
    public string OutputPath           { get; set; } = string.Empty;
}

public sealed partial class ExportService : ObservableObject
{
    [ObservableProperty] private bool _isExporting;
    [ObservableProperty] private double _progress;
    [ObservableProperty] private string _statusMessage = string.Empty;

    private CancellationTokenSource? _cts;

    public async Task<bool> ExportAsync(Timeline timeline, ExportOptions options)
    {
        if (IsExporting) return false;

        IsExporting     = true;
        Progress        = 0;
        StatusMessage   = "Building composition…";
        _cts            = new CancellationTokenSource();

        try
        {
            var preset = BuildPreset(timeline, options);
            var builder = new CompositionBuilder();

            var prog = new Progress<double>(p =>
            {
                Progress      = p;
                StatusMessage = $"Encoding… {p:P0}";
            });

            await builder.BuildAsync(timeline, options.OutputPath, preset, prog, _cts.Token);
            StatusMessage = "Export complete.";
            Log.Export.LogInformation("Exported to {path}", options.OutputPath);
            return true;
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Export cancelled.";
            return false;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export failed: {ex.Message}";
            Log.Export.LogError(ex, "Export failed");
            return false;
        }
        finally
        {
            IsExporting = false;
        }
    }

    public void Cancel() => _cts?.Cancel();

    private static ExportPreset BuildPreset(Timeline timeline, ExportOptions options)
    {
        var (w, h) = options.Resolution switch
        {
            ExportResolution.R720p  => (1280,  720),
            ExportResolution.R1080p => (1920, 1080),
            ExportResolution.R4K    => (3840, 2160),
            ExportResolution.Custom => (options.CustomWidth, options.CustomHeight),
            _ => (timeline.Width, timeline.Height)
        };

        return options.Format switch
        {
            ExportFormat.H265   => new ExportPreset { Width = w, Height = h, Fps = timeline.Fps, VideoCodec = "libx265", Crf = "20" },
            ExportFormat.ProRes => new ExportPreset { Width = w, Height = h, Fps = timeline.Fps, VideoCodec = "prores_ks", AudioCodec = "pcm_s16le" },
            _                   => new ExportPreset { Width = w, Height = h, Fps = timeline.Fps }
        };
    }
}
