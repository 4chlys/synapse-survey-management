namespace SynapseEnqueteManagement.API.DTOs.Stats;

public class PlatformStatsRowDto
{
    public int PlatformId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ActiveCount { get; set; }
    public int DraftCount { get; set; }
    public int ArchivedCount { get; set; }
    public int ParticipantCount { get; set; }
    public double AvgCompletionSeconds { get; set; }
    public double MedianCompletionSeconds { get; set; }
}
