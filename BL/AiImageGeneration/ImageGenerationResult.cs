namespace SynapseEnqueteManagement.BL.AiImageGeneration;

public sealed record ImageGenerationResult
{
    public bool IsSuccess { get; private init; }
    public byte[] ImageBytes { get; private init; }
    public string MimeType { get; private init; }
    public string ErrorMessage { get; private init; }

    public static ImageGenerationResult Success(byte[] imageBytes, string mimeType) =>
        new() { IsSuccess = true, ImageBytes = imageBytes, MimeType = mimeType };

    public static ImageGenerationResult Failed(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };
}
