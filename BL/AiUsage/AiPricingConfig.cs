namespace SynapseEnqueteManagement.BL.AiUsage;

public class AiPricingConfig
{
    public Dictionary<string, ModelPrice> Models { get; set; } = new();
}

public record ModelPrice(decimal InputPerMillion, decimal OutputPerMillion, decimal PerImage);
