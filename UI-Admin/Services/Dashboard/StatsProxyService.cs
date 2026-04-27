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
}
