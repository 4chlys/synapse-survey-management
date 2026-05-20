#nullable enable
namespace SynapseEnqueteManagement.UI.Admin.Services.Dashboard;

public class SocialMediaProxyService(HttpClient httpClient)
{
    public async Task<(int StatusCode, string Content)> PostAsync(string path, HttpContent? content = null)
    {
        try
        {
            var response = await httpClient.PostAsync(path, content);
            var body     = await response.Content.ReadAsStringAsync();
            return ((int)response.StatusCode, body);
        }
        catch
        {
            return (502, string.Empty);
        }
    }
}
