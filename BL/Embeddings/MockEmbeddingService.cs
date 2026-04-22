using System.Security.Cryptography;
using System.Text;

namespace SynapseEnqueteManagement.BL.Embeddings;

// Deterministic, dependency-free embedding producer for dev/CI. Hashes tokens
// into a bag-of-words float vector, then L2-normalises. Same text → same vector,
// similar texts (shared words) → higher cosine similarity. Not semantic — a
// stand-in so the rest of the pipeline can run without a real provider.
public class MockEmbeddingService(int dimensions = 128) : IEmbeddingService
{
    private static readonly char[] Separators =
        [' ', '\t', '\n', '\r', '.', ',', ';', ':', '!', '?', '-', '(', ')', '[', ']', '/', '\\', '"', '\''];

    public int Dimensions { get; } = dimensions;

    public Task<float[]> GenerateAsync(string text, CancellationToken cancellationToken = default)
        => Task.FromResult(Embed(text));

    public Task<IReadOnlyList<float[]>> GenerateBatchAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default)
    {
        var result = new float[texts.Count][];
        for (int i = 0; i < texts.Count; i++) result[i] = Embed(texts[i]);
        return Task.FromResult<IReadOnlyList<float[]>>(result);
    }

    private float[] Embed(string text)
    {
        var vector = new float[Dimensions];
        if (string.IsNullOrWhiteSpace(text)) return vector;

        foreach (var raw in text.ToLowerInvariant().Split(Separators, StringSplitOptions.RemoveEmptyEntries))
        {
            // SHA-256 gives a uniform hash that splits cleanly across buckets —
            // GetHashCode() is unstable across runs and would break determinism.
            Span<byte> hash = stackalloc byte[32];
            SHA256.HashData(Encoding.UTF8.GetBytes(raw), hash);
            int bucket = (int)(BitConverter.ToUInt32(hash[..4]) % (uint)Dimensions);
            int signBit = hash[4] & 1;
            vector[bucket] += signBit == 0 ? 1f : -1f;
        }

        float norm = 0f;
        for (int i = 0; i < vector.Length; i++) norm += vector[i] * vector[i];
        if (norm > 0f)
        {
            float inv = 1f / MathF.Sqrt(norm);
            for (int i = 0; i < vector.Length; i++) vector[i] *= inv;
        }
        return vector;
    }
}
