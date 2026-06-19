using System.Text.Json;
using qutCUT.Utilities;

namespace qutCUT.Transcription;

public sealed class TranscriptCache(string directory)
{
    public TranscriptionResult? Load(string assetId)
    {
        var path = CachePath(assetId);
        if (!File.Exists(path)) return null;
        try { return JsonSerializer.Deserialize<TranscriptionResult>(File.ReadAllText(path), JsonOptions.Default); }
        catch { return null; }
    }

    public void Save(string assetId, TranscriptionResult result)
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(CachePath(assetId), JsonSerializer.Serialize(result, JsonOptions.Default));
    }

    public void Invalidate(string assetId)
    {
        var path = CachePath(assetId);
        if (File.Exists(path)) File.Delete(path);
    }

    private string CachePath(string id) => Path.Combine(directory, $"{id}.transcript.json");
}
