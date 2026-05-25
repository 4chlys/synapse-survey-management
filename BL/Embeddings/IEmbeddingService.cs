namespace SynapseEnqueteManagement.BL.Embeddings;

// Abstraction over an embedding provider (OpenAI, Vertex AI, Ollama, …).
public interface IEmbeddingService
{
    /// <summary>
    /// Generates an embedding vector for <paramref name="text"/>. Pass a positive
    /// <paramref name="projectId"/> to record cost against that project; pass 0
    /// for system calls (e.g. seed-time backfill) that should not be billed.
    /// </summary>
    Task<float[]> GenerateAsync(string text, int projectId, CancellationToken cancellationToken = default);
}
