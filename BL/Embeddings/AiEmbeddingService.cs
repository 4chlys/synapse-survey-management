using Microsoft.Extensions.AI;

namespace SynapseEnqueteManagement.BL.Embeddings;

public class AiEmbeddingService(IEmbeddingGenerator<string, Embedding<float>> generator) : IEmbeddingService
{
    public async Task<float[]> GenerateAsync(string text, CancellationToken ct = default)
    {
        var results = await generator.GenerateAsync([text], cancellationToken: ct);
        return results[0].Vector.ToArray();
    }
}
