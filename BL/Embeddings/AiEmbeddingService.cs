using SynapseEnqueteManagement.BL.Embeddings;

namespace SynapseEnqueteManagement.BL.Embeddings;

public class AiEmbeddingService : IEmbeddingService
{
    public int Dimensions { get; set; }

    public Task<float[]> GenerateAsync(string text, CancellationToken ct = default)
    {

    }
    
    public Task<float[]> GenerateBatchAsync(IReadOnlyList<string> texts, CancellationToken ct = default)
    {
        
    }
}
