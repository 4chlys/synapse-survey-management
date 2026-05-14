namespace SynapseEnqueteManagement.UI.User.Services.IdeaImage;

public interface IIdeaImageService
{
    Task<IdeaImageUploadResult> UploadAsync(IFormFile file, int projectId, CancellationToken ct = default);
    Task<ImageDescriptionResult> DescribeAsync(IFormFile file, int projectId, string categoryName, string categoryDescription, string language, CancellationToken ct = default);
}

public class IdeaImageUploadResult
{
    public bool IsSuccess { get; init; }
    public bool IsAcceptable { get; init; }
    public string Url { get; init; }
    public string ImageModerationToken { get; init; }
    public string Reason { get; init; }

    public static IdeaImageUploadResult Success(string url, string token) =>
        new() { IsSuccess = true, IsAcceptable = true, Url = url, ImageModerationToken = token };

    public static IdeaImageUploadResult AiRejected(string url, string token, string reason) =>
        new() { IsSuccess = true, IsAcceptable = false, Url = url, ImageModerationToken = token, Reason = reason };

    public static IdeaImageUploadResult Error(string reason) =>
        new() { IsSuccess = false, Reason = reason };
}

public class ImageDescriptionResult
{
    public bool IsSuccess { get; init; }
    public string Description { get; init; }
    public string Reason { get; init; }

    public static ImageDescriptionResult Success(string description) =>
        new() { IsSuccess = true, Description = description };

    public static ImageDescriptionResult Error(string reason) =>
        new() { IsSuccess = false, Reason = reason };
}
