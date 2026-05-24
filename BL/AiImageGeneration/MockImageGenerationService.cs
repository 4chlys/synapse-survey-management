namespace SynapseEnqueteManagement.BL.AiImageGeneration;

public sealed class MockImageGenerationService : IAiImageGenerationService
{
    public Task<ImageGenerationResult> GenerateProjectImageAsync(
        string projectName,
        int projectId,
        string projectTopic = null,
        string introText = null,
        CancellationToken ct = default)
    {
        return Task.FromResult(ImageGenerationResult.Failed("AI image generation is disabled."));
    }
}
