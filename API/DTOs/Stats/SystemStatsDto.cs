using SynapseEnqueteManagement.API.DTOs.AiUsage;

namespace SynapseEnqueteManagement.API.DTOs.Stats;

public record SystemStatsDto
{
    public int PlatformCount { get; init; }
    public int ActiveProjectCount { get; init; }
    public int DraftProjectCount { get; init; }
    public int ArchivedProjectCount { get; init; }
    public int TotalParticipants { get; init; }
    public double AvgCompletionSeconds { get; init; }
    public double MedianCompletionSeconds { get; init; }
    public ICollection<PlatformStatsRowDto> Platforms { get; init; } = [];
    public ICollection<ProjectStatsRowDto> Projects { get; init; } = [];
    public ICollection<ServiceCostAggregateDto> AiUsageBySystem { get; init; } = [];
    public ICollection<PlatformServiceCostAggregateDto> AiUsageByPlatform { get; init; } = [];
    public ICollection<ProjectServiceCostAggregateDto> AiUsageByProject { get; init; } = [];
}
