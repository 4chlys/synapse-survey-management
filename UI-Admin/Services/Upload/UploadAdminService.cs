namespace SynapseEnqueteManagement.UI.Admin.Services.Upload;

public class UploadAdminService(HttpClient httpClient)
{
    public async Task<string> UploadImageAsync(IFormFile file)
    {
        using var content = new MultipartFormDataContent();
        await using var stream = file.OpenReadStream();
        using var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
        content.Add(fileContent, "file", file.FileName);

        var response = await httpClient.PostAsync("api/upload", content);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<UploadResult>();
        return result!.Url;
    }

    private sealed record UploadResult
    {
        public string Url { get; init; } = string.Empty;
    }
}
