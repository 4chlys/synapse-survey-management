using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SynapseEnqueteManagement.BL.AiUsage;
using SynapseEnqueteManagement.BL.Domain.Ai.Entities;

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
        var pricing = Build(new() { ["gemini-1.5-flash"] = new ModelPrice(1m, 0m, 2m) });

        var cost = pricing.ComputeChat("gemini-1.5-flash", AiServiceKind.IdeaSupport, 1_000_000, 500_000);

        // (1M/1M)*1 + (500K/1M)*2 = 1 + 1 = 2
        Assert.Equal(2m, cost);
    }

    [Fact]
    public void ComputeChat_AudioInput_UsesAudioRate()
    {
        var pricing = Build(new() { ["gemini-1.5-flash"] = new ModelPrice(1m, 3m, 2m) });

        var cost = pricing.ComputeChat("gemini-1.5-flash", AiServiceKind.SpeechToText, 1_000_000, 0);

        // Audio rate: (1M/1M)*3 = 3
        Assert.Equal(3m, cost);
    }

    [Fact]
    public void ComputeChat_ZeroTokens_ReturnsZero()
    {
        var pricing = Build(new() { ["gemini-1.5-flash"] = new ModelPrice(1m, 0m, 2m) });

        Assert.Equal(0m, pricing.ComputeChat("gemini-1.5-flash", AiServiceKind.IdeaSupport, 0, 0));
    }

    [Fact]
    public void ComputeImage_KnownModel_UsesPixelAreaFormula()
    {
        // 1024×1024 is the reference resolution: 1290 tokens per image.
        // cost = (1 * 1290 / 1_000_000) * 30 = 0.0387
        var pricing = Build(new() { ["imagen-3"] = new ModelPrice(0m, 0m, 30m) });

        var cost = pricing.ComputeImage("imagen-3", 1, 1024, 1024);

        Assert.Equal(1290m / 1_000_000m * 30m, cost);
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

        Assert.Equal(0m, pricing.ComputeChat("unknown-model", AiServiceKind.IdeaSupport, 100, 100));
    }

    [Fact]
    public void ComputeImage_UnknownModel_ReturnsZero()
    {
        var pricing = Build(new());

        Assert.Equal(0m, pricing.ComputeImage("unknown-model", 5, 1024, 1024));
    }

    [Fact]
    public void ComputeEmbedding_UnknownModel_ReturnsZero()
    {
        var pricing = Build(new());

        Assert.Equal(0m, pricing.ComputeEmbedding("unknown-model", 500));
    }
}
