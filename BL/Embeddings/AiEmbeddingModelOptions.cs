namespace SynapseEnqueteManagement.BL.Embeddings;

/// <summary>
/// Holds the Vertex AI embedding model identifier (read from
/// <c>VertexAi:EmbeddingModel</c> in configuration) so usage logging records
/// which model was billed.
/// </summary>
public sealed class AiEmbeddingModelOptions
{
    public string Model { get; init; } = "";
}
