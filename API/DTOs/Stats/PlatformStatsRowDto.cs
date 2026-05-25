namespace SynapseEnqueteManagement.API.DTOs.Stats;

public record PlatformStatsRowDto
{
    public int PlatformId { get; init; }
    public string Name { get; init; } = string.Empty;
    public int ActiveCount { get; init; }
    public int DraftCount { get; init; }
    public int ArchivedCount { get; init; }
    public int ParticipantCount { get; init; }
    public double AvgCompletionSeconds { get; init; }
    public double MedianCompletionSeconds { get; init; }
}
