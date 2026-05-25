using System.Net.Http.Json;

namespace SynapseEnqueteManagement.UI.Admin.Services.IdeaSummary;

public sealed record EnqueueSummaryRequest(
    string Format,
    string SamplingStrategy,
    int? CategoryId,
    string Status,
    string LanguageCode,
    string OutputLanguage,
    bool SelectAll);

public sealed record SummaryJobStatus(string Status, string ErrorMessage, bool HasOutput);

public sealed class IdeaSummaryAdminService(HttpClient httpClient)
{
    public async Task<Guid?> EnqueueAsync(int projectId, int platformId, EnqueueSummaryRequest req)
    {
        var response = await httpClient.PostAsJsonAsync(
            $"api/admin/projects/{projectId}/ideas/summary?platformId={platformId}", req);
        if (!response.IsSuccessStatusCode) return null;
        var result = await response.Content.ReadFromJsonAsync<EnqueueResult>();
        return result?.JobId;
    }

    public async Task<SummaryJobStatus> GetStatusAsync(int projectId, Guid jobId)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<SummaryJobStatus>(
                $"api/admin/projects/{projectId}/ideas/summary/{jobId}/status");
        }
        catch { return null; }
    }

    public async Task<(byte[] Bytes, string ContentType, string FileName)?> DownloadAsync(int projectId, Guid jobId)
    {
        var response = await httpClient.GetAsync(
            $"api/admin/projects/{projectId}/ideas/summary/{jobId}/download");
        if (!response.IsSuccessStatusCode) return null;
        var bytes = await response.Content.ReadAsByteArrayAsync();
        var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
                       ?? response.Content.Headers.ContentDisposition?.FileName
                       ?? "summary.pdf";
        return (bytes, contentType, fileName);
    }
}

file sealed record EnqueueResult(Guid JobId);