using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SynapseEnqueteManagement.BL.AiUsage;

public class AiPricing(IOptions<AiPricingConfig> options, ILogger<AiPricing> logger)
{
    private readonly AiPricingConfig _config = options.Value;

    public decimal ComputeChat(string model, int inputTokens, int outputTokens)
    {
        if (!_config.Models.TryGetValue(model, out var price))
        {
            logger.LogWarning("No pricing configured for model {Model}; cost will be 0", model);
            return 0m;
        }
        return (inputTokens / 1_000_000m) * price.InputPerMillion
             + (outputTokens / 1_000_000m) * price.OutputPerMillion;
    }

    public decimal ComputeImage(string model, int count)
    {
        if (!_config.Models.TryGetValue(model, out var price))
        {
            logger.LogWarning("No pricing configured for model {Model}; cost will be 0", model);
            return 0m;
        }
        return count * price.PerImage;
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
