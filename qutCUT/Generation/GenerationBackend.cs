using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using qutCUT.Models;

namespace qutCUT.Generation;

// HTTP client for the Higgsfield/Palmier generation backend
public sealed class GenerationBackend : IDisposable
{
    private readonly HttpClient _http;

    public GenerationBackend(string baseUrl, string authToken)
    {
        _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", authToken);
        _http.Timeout = TimeSpan.FromMinutes(2);
    }

    public async Task<string?> SubmitAsync(GenerationRequest request, CancellationToken ct = default)
    {
        var payload = new
        {
            type            = request.Type.ToString().ToLowerInvariant(),
            prompt          = request.Prompt,
            negativePrompt  = request.NegativePrompt,
            model           = request.Model,
            durationSeconds = request.DurationSeconds,
            aspectRatio     = request.AspectRatio,
            extra           = request.Extra
        };

        var json     = JsonSerializer.Serialize(payload);
        var content  = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _http.PostAsync("/api/generate", content, ct);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(ct);
        var doc  = JsonDocument.Parse(body);
        return doc.RootElement.TryGetProperty("jobId", out var jobProp)
            ? jobProp.GetString()
            : null;
    }

    public async Task<JobStatusInfo?> GetStatusAsync(string jobId, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"/api/jobs/{jobId}", ct);
        if (!response.IsSuccessStatusCode) return null;

        var body = await response.Content.ReadAsStringAsync(ct);
        var doc  = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var statusStr = root.TryGetProperty("status", out var sp) ? sp.GetString() : null;
        var status    = statusStr switch
        {
            "ready"      => GenerationStatus.Ready,
            "processing" => GenerationStatus.Processing,
            "failed"     => GenerationStatus.Failed,
            _            => GenerationStatus.Pending
        };

        return new JobStatusInfo
        {
            Status    = status,
            Progress  = root.TryGetProperty("progress", out var pp) ? pp.GetDouble() : 0,
            ResultUrl = root.TryGetProperty("resultUrl", out var rp) ? rp.GetString() : null,
            Error     = root.TryGetProperty("error", out var ep) ? ep.GetString() : null
        };
    }

    public async Task<string?> UploadReferenceAsync(string filePath, CancellationToken ct = default)
    {
        await using var fs = File.OpenRead(filePath);
        var content  = new MultipartFormDataContent();
        content.Add(new StreamContent(fs), "file", Path.GetFileName(filePath));
        var response = await _http.PostAsync("/api/media/upload", content, ct);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync(ct);
        var doc  = JsonDocument.Parse(body);
        return doc.RootElement.TryGetProperty("url", out var up) ? up.GetString() : null;
    }

    public void Dispose() => _http.Dispose();
}
