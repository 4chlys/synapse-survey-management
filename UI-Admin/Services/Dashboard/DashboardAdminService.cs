using System.Net.Http.Json;
using SynapseEnqueteManagement.UI.Admin.Models.Dashboard;

namespace SynapseEnqueteManagement.UI.Admin.Services.Dashboard;

public class DashboardAdminService(HttpClient httpClient)
{
    public async Task<DashboardViewModel> GetDashboardAsync(int projectId, string projectName)
    {
        var response = await httpClient.GetAsync($"api/projects/{projectId}/dashboard");
        if (!response.IsSuccessStatusCode)
            return new DashboardViewModel { ProjectId = projectId, ProjectName = projectName };

        var dto = await response.Content.ReadFromJsonAsync<DashboardApiResponse>();
        if (dto is null)
            return new DashboardViewModel { ProjectId = projectId, ProjectName = projectName };

        return new DashboardViewModel
        {
            ProjectId = projectId,
            ProjectName = projectName,
            ViewCount = dto.ViewCount,
            CompletionCount = dto.CompletionCount,
            CompletionRate = dto.CompletionRate,
            TopCategories = dto.TopCategories.Select(c => new CategoryMetricViewModel
            {
                CategoryId = c.CategoryId,
                Name = c.Name,
                IdeaCount = c.IdeaCount,
                TotalReactions = c.TotalReactions
            }).ToList(),
            TopIdeas = dto.TopIdeas.Select(i => new IdeaMetricViewModel
            {
                IdeaId = i.IdeaId,
                Description = i.Description,
                ReactionCount = i.ReactionCount,
                CommentCount = i.CommentCount
            }).ToList()
        };
    }

    // Private DTO records matching the API response shape
    private record DashboardApiResponse
    {
        public int ProjectId { get; init; }
        public int ViewCount { get; init; }
        public int CompletionCount { get; init; }
        public double CompletionRate { get; init; }
        public List<CategoryApiDto> TopCategories { get; init; } = new();
        public List<IdeaApiDto> TopIdeas { get; init; } = new();
    }

    private record CategoryApiDto
    {
        public int CategoryId { get; init; }
        public string Name { get; init; }
        public int IdeaCount { get; init; }
        public int TotalReactions { get; init; }
    }

    private record IdeaApiDto
    {
        public int IdeaId { get; init; }
        public string Description { get; init; }
        public int ReactionCount { get; init; }
        public int CommentCount { get; init; }
    }
}
