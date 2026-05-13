using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SynapseEnqueteManagement.BL.AiUsage;

namespace SynapseEnqueteManagement.BL.Tests.Unit;

public class AiPricingTests
{
    private static AiPricing Build(Dictionary<string, ModelPrice> models)
    {
        var config = new AiPricingConfig { Models = models };
        return new AiPricing(Options.Create(config), NullLogger<AiPricing>.Instance);
    }

    [Fact]
    public void ComputeChat_KnownModel_ReturnsCorrectCost()
    {
        var pricing = Build(new() { ["gemini-1.5-flash"] = new ModelPrice(1m, 2m, 0m) });

        var cost = pricing.ComputeChat("gemini-1.5-flash", 1_000_000, 500_000);

        // (1M/1M)*1 + (500K/1M)*2 = 1 + 1 = 2
        Assert.Equal(2m, cost);
    }

    [Fact]
    public void ComputeChat_ZeroTokens_ReturnsZero()
    {
        var pricing = Build(new() { ["gemini-1.5-flash"] = new ModelPrice(1m, 2m, 0m) });

        Assert.Equal(0m, pricing.ComputeChat("gemini-1.5-flash", 0, 0));
    }

    [Fact]
    public void ComputeImage_KnownModel_ReturnsCountTimesPerImage()
    {
        var pricing = Build(new() { ["imagen-3"] = new ModelPrice(0m, 0m, 0.04m) });

        Assert.Equal(0.12m, pricing.ComputeImage("imagen-3", 3));
    }

    [Fact]
    public void ComputeEmbedding_KnownModel_ReturnsCorrectCost()
    {
        var pricing = Build(new() { ["text-embedding-004"] = new ModelPrice(0.025m, 0m, 0m) });

        // (2M/1M)*0.025 = 0.05
        Assert.Equal(0.05m, pricing.ComputeEmbedding("text-embedding-004", 2_000_000));
    }

    [Fact]
    public void ComputeChat_UnknownModel_ReturnsZero()
    {
        var pricing = Build(new());

        Assert.Equal(0m, pricing.ComputeChat("unknown-model", 100, 100));
    }

    [Fact]
    public void ComputeImage_UnknownModel_ReturnsZero()
    {
        var pricing = Build(new());

        Assert.Equal(0m, pricing.ComputeImage("unknown-model", 5));
    }

    [Fact]
    public void ComputeEmbedding_UnknownModel_ReturnsZero()
    {
        var pricing = Build(new());

        Assert.Equal(0m, pricing.ComputeEmbedding("unknown-model", 500));
    }
}
