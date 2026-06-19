using Windows.Media.SpeechRecognition;
using qutCUT.Utilities;

namespace qutCUT.Transcription;

public sealed class TranscriptWord
{
    public string Text       { get; init; } = string.Empty;
    public TimeSpan Start    { get; init; }
    public TimeSpan End      { get; init; }
    public double Confidence { get; init; }
}

public sealed class TranscriptSegment
{
    public string Text    { get; init; } = string.Empty;
    public TimeSpan Start { get; init; }
    public TimeSpan End   { get; init; }
    public List<TranscriptWord> Words { get; init; } = [];
}

public sealed class TranscriptionResult
{
    public string FullText   { get; init; } = string.Empty;
    public List<TranscriptSegment> Segments { get; init; } = [];

    public IEnumerable<TranscriptWord> AllWords =>
        Segments.SelectMany(s => s.Words);
}

// Windows equivalent of Transcription.swift (macOS Speech.framework).
// Uses Windows.Media.SpeechRecognition — built into Windows, no API key needed.
public sealed class TranscriptionService
{
    private readonly TranscriptCache _cache;

    public TranscriptionService(string cacheDirectory)
    {
        _cache = new TranscriptCache(cacheDirectory);
    }

    public async Task<TranscriptionResult?> TranscribeAsync(
        string mediaPath,
        string assetId,
        CancellationToken ct = default)
    {
        var cached = _cache.Load(assetId);
        if (cached is not null) return cached;

        // Extract audio to WAV for speech recognition
        var wavPath = Path.GetTempFileName() + ".wav";
        try
        {
            await ExtractAudioAsync(mediaPath, wavPath, ct);
            var result = await RecognizeAsync(wavPath, ct);
            if (result is not null)
                _cache.Save(assetId, result);
            return result;
        }
        finally
        {
            if (File.Exists(wavPath)) File.Delete(wavPath);
        }
    }

    private static async Task ExtractAudioAsync(string input, string wavOutput, CancellationToken ct)
    {
        // Validate before interpolating into FFmpeg args — a crafted filename like
        // `"; concat:http://evil/ #` would otherwise inject FFmpeg arguments/protocols.
        var safeInput  = MediaPathValidator.ValidateExists(input);
        var safeOutput = MediaPathValidator.Validate(wavOutput);  // output, may not exist yet

        var conv = Xabe.FFmpeg.FFmpeg.Conversions.New()
            .AddParameter($"-i \"{safeInput}\" -vn -ar 16000 -ac 1 -f wav \"{safeOutput}\" -y");
        await conv.Start(ct);
    }

    private static async Task<TranscriptionResult?> RecognizeAsync(string wavPath, CancellationToken ct)
    {
        try
        {
            var recognizer = new SpeechRecognizer();
            await recognizer.CompileConstraintsAsync();

            // Windows speech API works on audio files via StorageFile
            var file    = await Windows.Storage.StorageFile.GetFileFromPathAsync(wavPath);
            var stream  = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);

            var result  = await recognizer.RecognizeAsync();
            if (result?.Status != SpeechRecognitionResultStatus.Success)
                return null;

            // Build segment from result — Windows API returns word-level timing
            var words = result.GetAlternates(1).FirstOrDefault()?.Text
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select((w, i) => new TranscriptWord { Text = w, Confidence = 1.0 })
                .ToList() ?? [];

            var segment = new TranscriptSegment
            {
                Text  = result.Text,
                Words = words
            };

            return new TranscriptionResult
            {
                FullText = result.Text,
                Segments = [segment]
            };
        }
        catch (Exception ex)
        {
            Log.Transcription.LogError(ex, "Speech recognition failed for {path}", wavPath);
            return null;
        }
    }
}
