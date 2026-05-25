namespace SynapseEnqueteManagement.API.DTOs.Stats;

public record ProjectStatsRowDto
{
    public int ProjectId { get; init; }
    public string ProjectName { get; init; } = string.Empty;
    public string ShortForm { get; init; } = string.Empty;
    public int PlatformId { get; init; }
    public string PlatformName { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public int ParticipantCount { get; init; }
    public int StartedCount { get; init; }
    public double AvgCompletionSeconds { get; init; }
    public double MedianCompletionSeconds { get; init; }
    public double MinCompletionSeconds { get; init; }
    public double Q1CompletionSeconds { get; init; }
    public double Q3CompletionSeconds { get; init; }
    public double MaxCompletionSeconds { get; init; }
}
