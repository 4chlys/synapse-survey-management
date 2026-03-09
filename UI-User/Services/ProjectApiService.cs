
namespace SynapseEnqueteManagement.UI.User.Services;

public class ProjectApiService
{
    private readonly HttpClient _httpClient;

    public ProjectApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ProjectInfo> GetProjectAsync(int projectId)
    {
        var response = await _httpClient.GetAsync($"api/projects/{projectId}");
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<ProjectInfo>();
    }

    public class ProjectInfo
    {
        public int ProjectId { get; set; }
        public string ProjectState { get; set; } = string.Empty;
    }
}
