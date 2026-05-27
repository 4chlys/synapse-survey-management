using System.Net.Http.Json;
using SynapseEnqueteManagement.UI.Admin.Models.Enums;

namespace SynapseEnqueteManagement.UI.Admin.Services.Question;

public class QuestionMediaService(HttpClient httpClient) : IQuestionMediaService
{
    public async Task<QuestionMediaUploadResult> UploadAsync(IFormFile file, QuestionMediaType type, CancellationToken ct)
    {
        using var content = new MultipartFormDataContent();
        await using var stream = file.OpenReadStream();
        using var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
        content.Add(fileContent, "file", file.FileName);

        var response = await httpClient.PostAsync(
            $"api/upload/question-media?mediaType={(int)type}", content, ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            return new QuestionMediaUploadResult { Success = false, Error = error };
        }

        var result = await response.Content.ReadFromJsonAsync<UploadResult>(cancellationToken: ct);
        return new QuestionMediaUploadResult { Success = true, Url = result!.Url };
    }

    private sealed record UploadResult
    {
        public string Url { get; init; }
    }
}
