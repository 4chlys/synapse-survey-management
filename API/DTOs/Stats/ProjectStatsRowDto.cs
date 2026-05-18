namespace SynapseEnqueteManagement.API.DTOs.Stats;

public class ProjectStatsRowDto
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string ShortForm { get; set; } = string.Empty;
    public int PlatformId { get; set; }
    public string PlatformName { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public int ParticipantCount { get; set; }
    public int StartedCount { get; set; }
    public double AvgCompletionSeconds { get; set; }
    public double MedianCompletionSeconds { get; set; }
    public double MinCompletionSeconds { get; set; }
    public double Q1CompletionSeconds { get; set; }
    public double Q3CompletionSeconds { get; set; }
    public double MaxCompletionSeconds { get; set; }
}
