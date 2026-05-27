using System.Net.Http.Json;
using SynapseEnqueteManagement.UI.Admin.Models.Platform;

namespace SynapseEnqueteManagement.UI.Admin.Services.Platform;

// HTTP client for the API's /api/admin/platforms endpoints. Replaces UI-Admin's
// previous EF-based access to the ProjectPlatforms table — UI-Admin no longer owns
// any C# model of that table, only the local PlatformDto returned here.
public class PlatformApiService(HttpClient httpClient)
{
    private const string Base = "api/admin/platforms";

    public async Task<List<PlatformDto>> GetAllAsync(CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync(Base, ct);
        if (!response.IsSuccessStatusCode) return new List<PlatformDto>();
        return await response.Content.ReadFromJsonAsync<List<PlatformDto>>(cancellationToken: ct)
               ?? new List<PlatformDto>();
    }

    public async Task<PlatformDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"{Base}/{id}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PlatformDto>(cancellationToken: ct);
    }

    public async Task<PlatformDto> CreateAsync(string name, string logoUrl, string websiteUrl, CancellationToken ct = default)
    {
        var payload = new { Name = name, LogoUrl = logoUrl, WebsiteUrl = websiteUrl };
        var response = await httpClient.PostAsJsonAsync(Base, payload, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PlatformDto>(cancellationToken: ct);
    }

    public async Task<bool> UpdateAsync(int id, string name, string logoUrl, string websiteUrl, CancellationToken ct = default)
    {
        var payload = new { Name = name, LogoUrl = logoUrl, WebsiteUrl = websiteUrl };
        var response = await httpClient.PutAsJsonAsync($"{Base}/{id}", payload, ct);
        return response.IsSuccessStatusCode;
    }
}
