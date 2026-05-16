using System.Net.Http.Json;

namespace SynapseEnqueteManagement.UI.Admin.Services.Ai;

public class AiAdminService(HttpClient httpClient)
{
    public async Task<HashSet<string>> GetAvailableModelIdsAsync()
    {
        try
        {
            var ids = await httpClient.GetFromJsonAsync<List<string>>("api/admin/ai/available-models");
            return (ids ?? []).ToHashSet();
        }
        catch
        {
            return [];
        }
    }
}