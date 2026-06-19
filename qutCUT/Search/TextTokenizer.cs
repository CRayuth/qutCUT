using System.Text;
using System.Text.RegularExpressions;

namespace qutCUT.Search;

// BPE tokenizer matching the macOS HuggingFace swift-transformers implementation.
// Loads vocab.json + merges.txt from the CLIP tokenizer bundle.
public sealed class TextTokenizer
{
    private Dictionary<string, int> _vocab = [];
    private List<(string, string)>  _merges = [];
    private readonly int _contextLen;
    private const int BosTokenId = 49406;
    private const int EosTokenId = 49407;

    public TextTokenizer(string vocabPath, string mergesPath, int contextLen = 77)
    {
        _contextLen = contextLen;
        LoadVocab(vocabPath);
        LoadMerges(mergesPath);
    }

    public long[] Tokenize(string text)
    {
        var tokens = new List<int> { BosTokenId };
        var words = Regex.Split(text.ToLowerInvariant().Trim(), @"\s+");
        foreach (var word in words)
        {
            var chars = word.Select(c => c.ToString()).ToList();
            if (chars.Count == 0) continue;
            chars[^1] += "</w>";
            var bpe = BpeEncode(chars);
            foreach (var t in bpe)
            {
                if (_vocab.TryGetValue(t, out var id))
                    tokens.Add(id);
            }
        }

        tokens.Add(EosTokenId);

        // Pad / truncate to context length
        while (tokens.Count < _contextLen) tokens.Add(0);
        if (tokens.Count > _contextLen) tokens = tokens[.._contextLen];

        return tokens.Select(t => (long)t).ToArray();
    }

    private List<string> BpeEncode(List<string> chars)
    {
        var word = new List<string>(chars);
        while (word.Count > 1)
        {
            var bigram = FindBestBigram(word);
            if (bigram is null) break;
            word = ApplyMerge(word, bigram.Value.Item1, bigram.Value.Item2);
        }
        return word;
    }

    private (string, string)? FindBestBigram(List<string> word)
    {
        (string, string)? best = null;
        int bestRank = int.MaxValue;
        for (int i = 0; i < word.Count - 1; i++)
        {
            var pair = (word[i], word[i + 1]);
            var rank = _merges.IndexOf(pair);
            if (rank >= 0 && rank < bestRank) { bestRank = rank; best = pair; }
        }
        return best;
    }

    private static List<string> ApplyMerge(List<string> word, string left, string right)
    {
        var result = new List<string>();
        int i = 0;
        while (i < word.Count)
        {
            if (i < word.Count - 1 && word[i] == left && word[i + 1] == right)
            {
                result.Add(left + right);
                i += 2;
            }
            else { result.Add(word[i++]); }
        }
        return result;
    }

    private void LoadVocab(string path)
    {
        if (!File.Exists(path)) return;
        var json = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path));
        foreach (var kv in json.RootElement.EnumerateObject())
            _vocab[kv.Name] = kv.Value.GetInt32();
    }

    private void LoadMerges(string path)
    {
        if (!File.Exists(path)) return;
        _merges = File.ReadLines(path)
            .Skip(1) // skip header
            .Select(l => { var p = l.Split(' '); return (p[0], p[1]); })
            .Where(p => !string.IsNullOrEmpty(p.Item1) && !string.IsNullOrEmpty(p.Item2))
            .ToList();
    }
}
