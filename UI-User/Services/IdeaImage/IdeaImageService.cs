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

        if (response.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
        {
            var err = await response.Content.ReadFromJsonAsync<RejectionBody>(cancellationToken: ct);
            return IdeaImageUploadResult.Rejected(err?.Reason ?? "De afbeelding is niet toegestaan.");
        }

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<UploadBody>(cancellationToken: ct);
        return IdeaImageUploadResult.Success(result!.Url, result.ImageModerationToken);
    }

    private sealed class UploadBody
    {
        public string Url { get; set; }
        public string ImageModerationToken { get; set; }
    }

    private sealed class RejectionBody
    {
        public string Reason { get; set; }
    }
}
