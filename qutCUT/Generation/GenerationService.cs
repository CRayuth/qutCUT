using CommunityToolkit.Mvvm.ComponentModel;
using qutCUT.Models;
using qutCUT.Utilities;

namespace qutCUT.Generation;

public sealed partial class GenerationService : ObservableObject
{
    [ObservableProperty] private List<string> _activeJobIds = [];

    private readonly GenerationBackend _backend;
    private readonly string _downloadDirectory;
    private readonly Dictionary<string, CancellationTokenSource> _pollers = [];

    public event Action<string, GenerationStatus, double>? JobStatusChanged;
    public event Action<string, string>? JobCompleted;   // assetId, localPath
    public event Action<string, string>? JobFailed;      // assetId, error

    public GenerationService(string downloadDirectory, string backendBaseUrl, string authToken)
    {
        _downloadDirectory = downloadDirectory;
        _backend           = new GenerationBackend(backendBaseUrl, authToken);
        Directory.CreateDirectory(downloadDirectory);
    }

    public async Task<string?> SubmitAsync(GenerationRequest request, CancellationToken ct = default)
    {
        try
        {
            var jobId = await _backend.SubmitAsync(request, ct);
            if (jobId is null) return null;

            Log.Generation.LogInformation("Job submitted: {id} ({type})", jobId, request.Type);

            var cts = new CancellationTokenSource();
            _pollers[request.AssetId] = cts;
            ActiveJobIds = [.. ActiveJobIds, request.AssetId];

            _ = PollJobAsync(request.AssetId, jobId, request.Type, cts.Token);
            return jobId;
        }
        catch (Exception ex)
        {
            Log.Generation.LogError(ex, "Submit failed for asset {id}", request.AssetId);
            return null;
        }
    }

    public void CancelJob(string assetId)
    {
        if (_pollers.TryGetValue(assetId, out var cts))
        {
            cts.Cancel();
            _pollers.Remove(assetId);
            ActiveJobIds = ActiveJobIds.Where(id => id != assetId).ToList();
        }
    }

    private async Task PollJobAsync(string assetId, string jobId, GenerationType type, CancellationToken ct)
    {
        var delay = Constants.MinGenerationPollingIntervalMs;

        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(delay, ct);
            delay = Math.Min(delay * 2, Constants.MaxGenerationPollingIntervalMs);

            JobStatusInfo? status;
            try { status = await _backend.GetStatusAsync(jobId, ct); }
            catch { continue; }

            if (status is null) continue;

            JobStatusChanged?.Invoke(assetId, status.Status, status.Progress);

            if (status.Status == GenerationStatus.Ready && status.ResultUrl is not null)
            {
                var localPath = await DownloadAsync(assetId, status.ResultUrl, type, ct);
                _pollers.Remove(assetId);
                ActiveJobIds = ActiveJobIds.Where(id => id != assetId).ToList();

                if (localPath is not null)
                    JobCompleted?.Invoke(assetId, localPath);
                else
                    JobFailed?.Invoke(assetId, "Download failed");
                return;
            }

            if (status.Status == GenerationStatus.Failed)
            {
                _pollers.Remove(assetId);
                ActiveJobIds = ActiveJobIds.Where(id => id != assetId).ToList();
                JobFailed?.Invoke(assetId, status.Error ?? "Unknown error");
                return;
            }
        }
    }

    private async Task<string?> DownloadAsync(string assetId, string url, GenerationType type, CancellationToken ct)
    {
        var ext = type switch
        {
            GenerationType.Image => ".jpg",
            GenerationType.Audio => ".mp3",
            GenerationType.Music => ".mp3",
            _                    => ".mp4"
        };

        var dest = Path.Combine(_downloadDirectory, $"{assetId}{ext}");
        using var http = new System.Net.Http.HttpClient();
        try
        {
            var bytes = await http.GetByteArrayAsync(url, ct);
            await File.WriteAllBytesAsync(dest, bytes, ct);
            Log.Generation.LogInformation("Downloaded {id} → {path}", assetId, dest);
            return dest;
        }
        catch (Exception ex)
        {
            Log.Generation.LogError(ex, "Download failed for {id}", assetId);
            return null;
        }
    }
}

public sealed class GenerationRequest
{
    public string AssetId { get; set; } = Guid.NewGuid().ToString();
    public GenerationType Type { get; set; }
    public string Prompt { get; set; } = string.Empty;
    public string? NegativePrompt { get; set; }
    public string? Model { get; set; }
    public double? DurationSeconds { get; set; }
    public string? AspectRatio { get; set; }
    public string? ReferenceImagePath { get; set; }
    public Dictionary<string, object> Extra { get; set; } = [];
}

public enum GenerationType { Video, Image, Audio, Music }

public sealed class JobStatusInfo
{
    public GenerationStatus Status { get; set; }
    public double Progress { get; set; }
    public string? ResultUrl { get; set; }
    public string? Error { get; set; }
}
