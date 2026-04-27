namespace SynapseEnqueteManagement.API.DTOs.Dashboard;

public class ProjectDashboardDto
{
    public int ProjectId { get; set; }
    public int ViewCount { get; set; }
    public int CompletionCount { get; set; }
    public double CompletionRate { get; set; }
    public List<CategoryMetricDto> TopCategories { get; set; } = new();
    public List<IdeaMetricDto> TopIdeas { get; set; } = new();
}
