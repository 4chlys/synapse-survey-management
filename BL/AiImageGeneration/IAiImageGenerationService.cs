namespace SynapseEnqueteManagement.BL.AiImageGeneration;

public interface IAiImageGenerationService
{
    Task<ImageGenerationResult> GenerateProjectImageAsync(string projectName, CancellationToken ct = default);
}
