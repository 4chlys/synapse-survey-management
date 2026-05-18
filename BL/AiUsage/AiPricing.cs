using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SynapseEnqueteManagement.BL.Domain.Ai.Entities;

namespace SynapseEnqueteManagement.BL.AiUsage;

public class AiPricing(IOptions<AiPricingConfig> options, ILogger<AiPricing> logger)
{
    private readonly AiPricingConfig _config = options.Value;

    public decimal ComputeChat(string model, AiServiceKind service, int inputTokens, int outputTokens)
    {
        if (!_config.Models.TryGetValue(model, out var price))
        {
            logger.LogWarning("No pricing configured for model {Model}; cost will be 0", model);
            return 0m;
        }
        // Audio input (STT) is billed at a higher rate than text/image input.
        var inputRate = service == AiServiceKind.SpeechToText
            ? price.AudioInputPerMillion
            : price.InputPerMillion;
        return (inputTokens / 1_000_000m) * inputRate
             + (outputTokens / 1_000_000m) * price.OutputPerMillion;
    }

    // Google bills image output at $30/1M tokens; 1024×1024 = 1290 tokens (reference).
    // Token count is proportional to pixel area, so we scale from the reference.
    public decimal ComputeImage(string model, int count, int width, int height)
    {
        if (!_config.Models.TryGetValue(model, out var price))
        {
            logger.LogWarning("No pricing configured for model {Model}; cost will be 0", model);
            return 0m;
        }
        int tokensPerImage = (int)Math.Round((long)width * height / 1_048_576.0 * 1290);
        return (count * tokensPerImage / 1_000_000m) * price.OutputPerMillion;
    }

    public decimal ComputeEmbedding(string model, int inputTokens)
    {
        if (!_config.Models.TryGetValue(model, out var price))
        {
            logger.LogWarning("No pricing configured for model {Model}; cost will be 0", model);
            return 0m;
        }
        return (inputTokens / 1_000_000m) * price.InputPerMillion;
    }
}
