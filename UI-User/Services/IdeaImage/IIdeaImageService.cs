namespace SynapseEnqueteManagement.UI.User.Services.IdeaImage;

public interface IIdeaImageService
{
    Task<IdeaImageUploadResult> UploadAsync(IFormFile file, int projectId, CancellationToken ct = default);
}

public class IdeaImageUploadResult
{
    public bool IsSuccess { get; init; }
    public string Url { get; init; }
    public string ImageModerationToken { get; init; }
    public string Reason { get; init; }

    public static IdeaImageUploadResult Success(string url, string token) =>
        new() { IsSuccess = true, Url = url, ImageModerationToken = token };

    public static IdeaImageUploadResult Rejected(string reason) =>
        new() { IsSuccess = false, Reason = reason };
}
