namespace SynapseEnqueteManagement.BL.AiImageDescription;

public sealed class MockImageDescriptionService : IAiImageDescriptionService
{
    public Task<string> DescribeImageAsync(byte[] imageBytes, string mimeType, string language,
        string categoryName, string categoryDescription, CancellationToken ct = default)
        => Task.FromResult(string.Empty);
}
