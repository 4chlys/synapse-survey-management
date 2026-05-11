namespace SynapseEnqueteManagement.BL.AiImageDescription;

public interface IAiImageDescriptionService
{
    Task<string> DescribeImageAsync(byte[] imageBytes, string mimeType, string language,
        string categoryName, string categoryDescription, CancellationToken ct = default);
}
