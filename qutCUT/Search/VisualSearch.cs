using qutCUT.Utilities;

namespace qutCUT.Search;

public sealed class SearchResult
{
    public string AssetId   { get; init; } = string.Empty;
    public float Similarity { get; init; }
    public long? FrameIndex { get; init; }
}

public sealed class VisualSearch(VisualEmbedder embedder, EmbeddingStore store)
{
    // Search by text query
    public List<SearchResult> SearchByText(string query, TextTokenizer tokenizer, int topK = 20)
    {
        var tokens    = tokenizer.Tokenize(query);
        var queryVec  = embedder.EmbedText(tokens);
        if (queryVec is null) return [];
        return RankResults(queryVec, topK);
    }

    // Search by reference image
    public List<SearchResult> SearchByImage(byte[] rgbPixels, int width, int height, int topK = 20)
    {
        var queryVec = embedder.EmbedImage(rgbPixels, width, height);
        if (queryVec is null) return [];
        return RankResults(queryVec, topK);
    }

    private List<SearchResult> RankResults(float[] query, int topK)
    {
        var results = new List<SearchResult>();
        foreach (var (assetId, frameIndex, embedding) in store.GetAll())
        {
            var sim = VisualEmbedder.CosineSimilarity(query, embedding);
            results.Add(new SearchResult { AssetId = assetId, Similarity = sim, FrameIndex = frameIndex });
        }
        return results.OrderByDescending(r => r.Similarity).Take(topK).ToList();
    }
}
