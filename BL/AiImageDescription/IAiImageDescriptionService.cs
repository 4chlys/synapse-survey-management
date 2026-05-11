using SynapseEnqueteManagement.BL.Ai;

namespace SynapseEnqueteManagement.BL.AiImageDescription;

public interface IAiImageDescriptionService
{
    Task<string> DescribeImageAsync(byte[] imageBytes, string mimeType, string language,
        string categoryName, string categoryDescription,
        ResolvedPlatformAiConfig platformConfig, CancellationToken ct = default);
}
