namespace SynapseEnqueteManagement.BL.AiImageGeneration;

public sealed class MockImageGenerationService : IAiImageGenerationService
{
    public Task<ImageGenerationResult> GenerateProjectImageAsync(string projectName, CancellationToken ct = default)
    {
        return Task.FromResult(ImageGenerationResult.Failed("AI-beeldgeneratie is uitgeschakeld."));
    }
}
