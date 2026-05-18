using SynapseEnqueteManagement.API.DTOs.Dashboard;

namespace SynapseEnqueteManagement.API.DTOs.Stats;

public class ProjectExportDataDto
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public int ViewCount { get; set; }
    public int CompletionCount { get; set; }
    public double CompletionRate { get; set; }
    public List<string> TimelineDates { get; set; } = [];
    public List<int> TimelineStarts { get; set; } = [];
    public List<int> TimelineCompletions { get; set; } = [];
    public List<CategoryMetricDto> TopCategories { get; set; } = [];
    public List<IdeaMetricDto> TopIdeas { get; set; } = [];
}
