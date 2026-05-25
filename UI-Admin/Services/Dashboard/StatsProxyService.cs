namespace SynapseEnqueteManagement.UI.Admin.Services.Dashboard;

public class StatsProxyService(HttpClient httpClient)
{
    public async Task<(int StatusCode, string Content)> ProxyGetAsync(string path)
    {
        try
        {
            var response = await httpClient.GetAsync(path);
            var body = await response.Content.ReadAsStringAsync();
            return ((int)response.StatusCode, body);
        }
        catch
        {
            return (502, string.Empty);
        }
    }

    public async Task<(int Status, Stream Content, string ContentType, string FileName)> ProxyGetFileAsync(string path)
    {
        try
        {
            var response = await httpClient.GetAsync(path, HttpCompletionOption.ResponseHeadersRead);
            var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
            var rawFileName = response.Content.Headers.ContentDisposition?.FileNameStar
                           ?? response.Content.Headers.ContentDisposition?.FileName;
            var fileName = rawFileName?.Trim('"') ?? "export";
            var stream = await response.Content.ReadAsStreamAsync();
            return ((int)response.StatusCode, stream, contentType, fileName);
        }
        catch
        {
            return (502, Stream.Null, "application/octet-stream", "export");
        }
    }
}
