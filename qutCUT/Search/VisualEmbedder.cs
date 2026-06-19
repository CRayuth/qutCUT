using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using qutCUT.Utilities;

namespace qutCUT.Search;

// Windows equivalent of VisualEmbedder.swift (CoreML CLIP dual-encoder).
// Uses ONNX Runtime + DirectML for GPU-accelerated inference.
public sealed class VisualEmbedder : IDisposable
{
    private InferenceSession? _imageEncoder;
    private InferenceSession? _textEncoder;
    private readonly int _embeddingDim;

    public bool IsLoaded => _imageEncoder is not null && _textEncoder is not null;

    public VisualEmbedder(int embeddingDim = 512)
    {
        _embeddingDim = embeddingDim;
    }

    public void LoadModels(string imageEncoderPath, string textEncoderPath)
    {
        var options = new SessionOptions();
        options.AppendExecutionProvider_DML(); // DirectML (GPU) — falls back to CPU
        options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;

        _imageEncoder = new InferenceSession(imageEncoderPath, options);
        _textEncoder  = new InferenceSession(textEncoderPath, options);
        Log.Search.LogInformation("ONNX encoders loaded (dim={dim})", _embeddingDim);
    }

    // Embed an image from raw pixel bytes (RGB, H×W×3).
    public float[]? EmbedImage(byte[] rgbPixels, int width, int height)
    {
        if (_imageEncoder is null) return null;
        try
        {
            var tensor = PreprocessImage(rgbPixels, width, height);
            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("pixel_values", tensor)
            };
            using var result = _imageEncoder.Run(inputs);
            var output = result.First().AsEnumerable<float>().ToArray();
            return Normalize(output);
        }
        catch (Exception ex)
        {
            Log.Search.LogError(ex, "Image embedding failed");
            return null;
        }
    }

    // Embed tokenized text (int32 token IDs).
    public float[]? EmbedText(long[] tokenIds)
    {
        if (_textEncoder is null) return null;
        try
        {
            var tensor = new DenseTensor<long>(tokenIds, [1, tokenIds.Length]);
            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input_ids", tensor)
            };
            using var result = _textEncoder.Run(inputs);
            var output = result.First().AsEnumerable<float>().ToArray();
            return Normalize(output);
        }
        catch (Exception ex)
        {
            Log.Search.LogError(ex, "Text embedding failed");
            return null;
        }
    }

    public static float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length) return 0;
        float dot = 0, normA = 0, normB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot  += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }
        var denom = Math.Sqrt(normA) * Math.Sqrt(normB);
        return denom < 1e-8f ? 0 : (float)(dot / denom);
    }

    private static DenseTensor<float> PreprocessImage(byte[] rgb, int width, int height)
    {
        // Resize to 224×224, normalize to ImageNet mean/std
        const int size = 224;
        var tensor = new DenseTensor<float>([1, 3, size, size]);

        float[] mean = [0.48145466f, 0.4578275f,  0.40821073f];
        float[] std  = [0.26862954f, 0.26130258f, 0.27577711f];

        // Simple nearest-neighbor resize
        for (int c = 0; c < 3; c++)
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            int srcX = x * width  / size;
            int srcY = y * height / size;
            int idx  = (srcY * width + srcX) * 3 + c;
            float pixel = rgb.Length > idx ? rgb[idx] / 255.0f : 0f;
            tensor[0, c, y, x] = (pixel - mean[c]) / std[c];
        }

        return tensor;
    }

    private static float[] Normalize(float[] vec)
    {
        float norm = (float)Math.Sqrt(vec.Sum(v => v * v));
        if (norm < 1e-8f) return vec;
        return vec.Select(v => v / norm).ToArray();
    }

    public void Dispose()
    {
        _imageEncoder?.Dispose();
        _textEncoder?.Dispose();
    }
}
