using SynapseEnqueteManagement.API.DTOs.Dashboard;

namespace SynapseEnqueteManagement.API.DTOs.Stats;

public class ProjectExportDataDto
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public int ViewCount { get; set; }
    public int CompletionCount { get; set; }
    public double CompletionRate { get; set; }
    public ICollection<string> TimelineDates { get; set; } = new List<string>();
    public ICollection<int> TimelineStarts { get; set; } = new List<int>();
    public ICollection<int> TimelineCompletions { get; set; } = new List<int>();
    public ICollection<CategoryMetricDto> TopCategories { get; set; } = new List<CategoryMetricDto>();
    public ICollection<IdeaMetricDto> TopIdeas { get; set; } = new List<IdeaMetricDto>();
}
