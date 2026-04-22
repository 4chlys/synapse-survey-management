namespace SynapseEnqueteManagement.BL.Embeddings;

// Abstraction over an embedding provider (OpenAI, Vertex AI, Ollama, …).
// Returned vectors must all share the same Dimensions for a given instance.
public interface IEmbeddingService
{
    int Dimensions { get; }

    Task<float[]> GenerateAsync(string text, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<float[]>> GenerateBatchAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default);
}
