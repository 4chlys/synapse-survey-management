using SynapseEnqueteManagement.UI.Admin.Models.Project;

namespace SynapseEnqueteManagement.UI.Admin.Services.Project;

public class IdeaAdminService(HttpClient httpClient)
{
    private class IdeaPageResponse
    {
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int ApprovedCount { get; set; }
        public int PendingCount { get; set; }
        public int FlaggedCount { get; set; }
        public List<IdeaRowViewModel> Items { get; set; } = new();
    }

    public async Task<(List<IdeaRowViewModel> Items, int Total, int Approved, int Pending, int Flagged)>
        GetIdeasAsync(int projectId, string search, int? categoryId, string status, int page, int pageSize = 20)
    {
        var url = $"api/admin/projects/{projectId}/ideas?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(search))    url += $"&search={Uri.EscapeDataString(search)}";
        if (categoryId.HasValue)                   url += $"&categoryId={categoryId}";
        if (!string.IsNullOrWhiteSpace(status))    url += $"&status={Uri.EscapeDataString(status)}";

        var response = await httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
            return (new List<IdeaRowViewModel>(), 0, 0, 0, 0);

        var result = await response.Content.ReadFromJsonAsync<IdeaPageResponse>()
                     ?? new IdeaPageResponse();

        return (result.Items, result.TotalCount, result.ApprovedCount, result.PendingCount, result.FlaggedCount);
    }

    public async Task SetStatusAsync(int projectId, int ideaId, string status)
    {
        var response = await httpClient.PatchAsJsonAsync(
            $"api/admin/projects/{projectId}/ideas/{ideaId}/status",
            new { Status = status });
        response.EnsureSuccessStatusCode();
    }

    public async Task RemoveIdeaImageAsync(int projectId, int ideaId)
    {
        var response = await httpClient.DeleteAsync(
            $"api/admin/projects/{projectId}/ideas/{ideaId}/image");
        response.EnsureSuccessStatusCode();
    }
}
