namespace SynapseEnqueteManagement.BL.Embeddings;

// Abstraction over an embedding provider (OpenAI, Vertex AI, Ollama, …).
public interface IEmbeddingService
{
    Task<float[]> GenerateAsync(string text, CancellationToken cancellationToken = default);
}
