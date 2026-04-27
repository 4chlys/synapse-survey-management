namespace SynapseEnqueteManagement.BL.Sorting.Embeddings;

// Kept in the BL so we don't drag Microsoft.ML / NumSharp in for three lines of math.
// For ≤1000 ideas × ~128-dim vectors a tight manual loop is well under a millisecond.
internal static class VectorMath
{
    public static float CosineSimilarity(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        if (a.Length != b.Length || a.Length == 0) return 0f;

        float dot = 0f, normA = 0f, normB = 0f;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }
        if (normA == 0f || normB == 0f) return 0f;
        return dot / (MathF.Sqrt(normA) * MathF.Sqrt(normB));
    }

    public static float[] Centroid(IReadOnlyList<float[]> vectors)
    {
        if (vectors.Count == 0) return [];

        int dim = vectors[0].Length;
        var centroid = new float[dim];
        int counted = 0;
        foreach (var v in vectors)
        {
            if (v == null || v.Length != dim) continue;
            for (int i = 0; i < dim; i++) centroid[i] += v[i];
            counted++;
        }
        if (counted == 0) return centroid;
        float inv = 1f / counted;
        for (int i = 0; i < dim; i++) centroid[i] *= inv;
        return centroid;
    }
}
