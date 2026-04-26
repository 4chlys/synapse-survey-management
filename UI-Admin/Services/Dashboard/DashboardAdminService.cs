using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using SynapseEnqueteManagement.UI.Admin.Models.Dashboard;

namespace SynapseEnqueteManagement.UI.Admin.Services.Dashboard;

public class DashboardAdminService(HttpClient httpClient, IConfiguration config)
{
    public async Task<DashboardViewModel> GetDashboardAsync(int projectId, string projectName)
    {
        var response = await httpClient.GetAsync($"api/projects/{projectId}/dashboard");
        if (!response.IsSuccessStatusCode)
            return new DashboardViewModel { ProjectId = projectId, ProjectName = projectName };

        var dto = await response.Content.ReadFromJsonAsync<DashboardApiResponse>();
        if (dto is null)
            return new DashboardViewModel { ProjectId = projectId, ProjectName = projectName };

        var surveyBase = config["SurveyBaseUrl"] ?? string.Empty;

        return new DashboardViewModel
        {
            ProjectId = projectId,
            ProjectName = projectName,
            ViewCount = dto.ViewCount,
            CompletionCount = dto.CompletionCount,
            CompletionRate = dto.CompletionRate,
            SurveyUrl = $"{surveyBase}/survey/{projectId}",
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

    // Private DTO classes matching the API response shape
    private class DashboardApiResponse
    {
        public int ProjectId { get; set; }
        public int ViewCount { get; set; }
        public int CompletionCount { get; set; }
        public double CompletionRate { get; set; }
        public List<CategoryApiDto> TopCategories { get; set; } = new();
        public List<IdeaApiDto> TopIdeas { get; set; } = new();
    }

    private class CategoryApiDto
    {
        public int CategoryId { get; set; }
        public string Name { get; set; }
        public int IdeaCount { get; set; }
        public int TotalReactions { get; set; }
    }

    private class IdeaApiDto
    {
        public int IdeaId { get; set; }
        public string Description { get; set; }
        public int ReactionCount { get; set; }
        public int CommentCount { get; set; }
    }
}
