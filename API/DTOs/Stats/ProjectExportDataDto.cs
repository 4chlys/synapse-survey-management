using SynapseEnqueteManagement.API.DTOs.Dashboard;

namespace SynapseEnqueteManagement.API.DTOs.Stats;

public record ProjectExportDataDto
{
    public int ProjectId { get; init; }
    public string ProjectName { get; init; } = string.Empty;
    public int ViewCount { get; init; }
    public int CompletionCount { get; init; }
    public double CompletionRate { get; init; }
    public ICollection<string> TimelineDates { get; init; } = new List<string>();
    public ICollection<int> TimelineStarts { get; init; } = new List<int>();
    public ICollection<int> TimelineCompletions { get; init; } = new List<int>();
    public ICollection<CategoryMetricDto> TopCategories { get; init; } = new List<CategoryMetricDto>();
    public ICollection<IdeaMetricDto> TopIdeas { get; init; } = new List<IdeaMetricDto>();
}
