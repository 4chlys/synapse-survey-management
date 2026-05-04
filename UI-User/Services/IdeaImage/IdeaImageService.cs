using System.Net.Http.Json;

namespace SynapseEnqueteManagement.UI.User.Services.IdeaImage;

public class IdeaImageService(HttpClient httpClient) : IIdeaImageService
{
    public async Task<IdeaImageUploadResult> UploadAsync(IFormFile file, int projectId, CancellationToken ct = default)
    {
        using var content = new MultipartFormDataContent();
        await using var stream = file.OpenReadStream();
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
        content.Add(fileContent, "file", file.FileName);

        var response = await httpClient.PostAsync($"api/upload/idea-image?projectId={projectId}", content, ct);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<UploadBody>(cancellationToken: ct);

        if (result!.IsAcceptable)
            return IdeaImageUploadResult.Success(result.Url, result.ImageModerationToken);

        return IdeaImageUploadResult.AiRejected(
            result.Url,
            result.ImageModerationToken,
            result.Reason ?? "De afbeelding is niet toegestaan.");
    }

    private sealed class UploadBody
    {
        public string Url { get; set; }
        public string ImageModerationToken { get; set; }
        public bool IsAcceptable { get; set; }
        public string Reason { get; set; }
    }
}
