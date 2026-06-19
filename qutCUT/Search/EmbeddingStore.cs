using System.Runtime.InteropServices;

namespace qutCUT.Search;

// Binary embedding store — mirrors the macOS Swift implementation exactly.
// Format: [entry count: int32] [entry: assetId(len+bytes) + frameIndex(int64) + embedding(float32[])]
public sealed class EmbeddingStore
{
    private readonly string _path;
    private readonly int _embeddingDim;
    private readonly List<(string assetId, long? frameIndex, float[] embedding)> _entries = [];

    public EmbeddingStore(string path, int embeddingDim = 512)
    {
        _path         = path;
        _embeddingDim = embeddingDim;
        Load();
    }

    public void Add(string assetId, long? frameIndex, float[] embedding)
    {
        _entries.Add((assetId, frameIndex, embedding));
    }

    public void RemoveAsset(string assetId) =>
        _entries.RemoveAll(e => e.assetId == assetId);

    public bool HasAsset(string assetId) =>
        _entries.Any(e => e.assetId == assetId);

    public IEnumerable<(string assetId, long? frameIndex, float[] embedding)> GetAll() =>
        _entries;

    public void Flush()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        using var fs = File.Open(_path, FileMode.Create);
        using var bw = new BinaryWriter(fs);

        bw.Write(_entries.Count);
        foreach (var (assetId, frameIndex, embedding) in _entries)
        {
            var idBytes = System.Text.Encoding.UTF8.GetBytes(assetId);
            bw.Write(idBytes.Length);
            bw.Write(idBytes);
            bw.Write(frameIndex ?? -1L);
            foreach (var v in embedding) bw.Write(v);
        }
    }

    private void Load()
    {
        if (!File.Exists(_path)) return;
        try
        {
            using var fs = File.OpenRead(_path);
            using var br = new BinaryReader(fs);

            var count = br.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var idLen    = br.ReadInt32();
                var assetId  = System.Text.Encoding.UTF8.GetString(br.ReadBytes(idLen));
                var rawFrame = br.ReadInt64();
                long? frame  = rawFrame < 0 ? null : rawFrame;
                var embedding = new float[_embeddingDim];
                for (int j = 0; j < _embeddingDim; j++) embedding[j] = br.ReadSingle();
                _entries.Add((assetId, frame, embedding));
            }
        }
        catch { _entries.Clear(); }
    }
}
