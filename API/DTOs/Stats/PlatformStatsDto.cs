namespace SynapseEnqueteManagement.API.DTOs.Stats;

public record PlatformStatsDto
{
    public int ActiveCount { get; init; }
    public int DraftCount { get; init; }
    public int ArchivedCount { get; init; }
    public int ParticipantCount { get; init; }
}