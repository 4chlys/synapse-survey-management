using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using SynapseEnqueteManagement.BL.AiUsage;

namespace SynapseEnqueteManagement.BL.Embeddings;

public class AiEmbeddingService(
    IEmbeddingGenerator<string, Embedding<float>> generator,
    IAiUsageManager usageManager,
    AiEmbeddingModelOptions modelOptions,
    ILogger<AiEmbeddingService> logger) : IEmbeddingService
{
    public async Task<float[]> GenerateAsync(string text, int projectId, CancellationToken ct = default)
    {
        var results = await generator.GenerateAsync([text], cancellationToken: ct);

        try
        {
            // Token count is not exposed on the embedding API surface; use a
            // coarse word-count estimate so cost is non-zero for billed calls.
            var inputTokens = string.IsNullOrWhiteSpace(text)
                ? 0
                : text.Split([' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries).Length;
            await usageManager.LogEmbeddingAsync(projectId, modelOptions.Model, inputTokens, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[Embedding] Failed to record AI usage.");
        }

        return results[0].Vector.ToArray();
    }
}
