namespace SynapseEnqueteManagement.UI.Admin.Services.Project;

public class ParticipantAdminService(HttpClient httpClient)
{
    private record EmailWithLanguage(string Email, string LanguageCode);

    // Flat distinct participant-email list, used by the broadcast "open in my
    // mail program" branch to build a mailto with everyone BCC'd. Reuses the
    // emails-by-language API endpoint and collapses the language grouping.
    public async Task<List<string>> GetParticipantEmailsAsync(int projectId)
    {
        var rows = await httpClient.GetFromJsonAsync<List<EmailWithLanguage>>(
            $"api/admin/projects/{projectId}/participants/emails-by-language");
        return rows?.Select(r => r.Email).Distinct().ToList() ?? new List<string>();
    }

    public async Task<(byte[] Bytes, string ContentType, string FileName)> ExportEmailsAsync(int projectId, string format)
    {
        var url = $"api/admin/projects/{projectId}/participants/emails/export?format={Uri.EscapeDataString(format)}";
        var response = await httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var bytes = await response.Content.ReadAsByteArrayAsync();
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
                       ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                       ?? $"participants-{projectId}.{format}";
        return (bytes, contentType, fileName);
    }

    public async Task<(byte[] Bytes, string ContentType, string FileName)> ExportRawDataAsync(int projectId, string format, string tables = "")
    {
        var url = $"api/admin/projects/{projectId}/rawdata/export?format={Uri.EscapeDataString(format)}";
        if (!string.IsNullOrWhiteSpace(tables))
            url += $"&tables={Uri.EscapeDataString(tables)}";
        var response = await httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var bytes = await response.Content.ReadAsByteArrayAsync();
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
                       ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                       ?? $"rawdata-{projectId}.{format}";
        return (bytes, contentType, fileName);
    }
}
