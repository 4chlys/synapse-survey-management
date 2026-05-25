namespace SynapseEnqueteManagement.API.DTOs.Dashboard;

public record ProjectDashboardDto
{
    public int ProjectId { get; init; }
    public int ViewCount { get; init; }
    public int CompletionCount { get; init; }
    public double CompletionRate { get; init; }
    public ICollection<CategoryMetricDto> TopCategories { get; init; } = new List<CategoryMetricDto>();
    public ICollection<IdeaMetricDto> TopIdeas { get; init; } = new List<IdeaMetricDto>();
}
