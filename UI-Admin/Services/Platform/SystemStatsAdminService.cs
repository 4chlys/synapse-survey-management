using System.Net.Http.Json;
using SynapseEnqueteManagement.UI.Admin.Models.Platform;

namespace SynapseEnqueteManagement.UI.Admin.Services.Platform;

public class SystemStatsAdminService(HttpClient httpClient)
{
    public async Task<SystemStatsDto> GetSystemStatsAsync(int? platformId)
    {
        var path = platformId.HasValue
            ? $"api/admin/platform-stats/system?platformId={platformId.Value}"
            : "api/admin/platform-stats/system";

        var response = await httpClient.GetAsync(path);
        if (!response.IsSuccessStatusCode)
            return new SystemStatsDto();

        return await response.Content.ReadFromJsonAsync<SystemStatsDto>() ?? new SystemStatsDto();
    }
}
