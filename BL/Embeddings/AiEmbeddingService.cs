using Microsoft.Extensions.AI;
using SynapseEnqueteManagement.BL.Embeddings;

namespace SynapseEnqueteManagement.BL.Embeddings;

public class AiEmbeddingService(IEmbeddingGenerator<string, Embedding<float>> generator) : IEmbeddingService
{
    public int Dimensions { get; set; }

    public async Task<float[]> GenerateAsync(string text, CancellationToken ct = default)
    {
        var results = await generator.GenerateAsync([text], cancellationToken: ct);
        return results[0].Vector.ToArray();
    }

    public Task<IReadOnlyList<float[]>> GenerateBatchAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
