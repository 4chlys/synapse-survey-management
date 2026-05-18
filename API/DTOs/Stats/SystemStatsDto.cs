using SynapseEnqueteManagement.API.DTOs.AiUsage;

namespace SynapseEnqueteManagement.API.DTOs.Stats;

public class SystemStatsDto
{
    public int PlatformCount { get; set; }
    public int ActiveProjectCount { get; set; }
    public int DraftProjectCount { get; set; }
    public int ArchivedProjectCount { get; set; }
    public int TotalParticipants { get; set; }
    public double AvgCompletionSeconds { get; set; }
    public double MedianCompletionSeconds { get; set; }
    public ICollection<PlatformStatsRowDto> Platforms { get; set; } = [];
    public ICollection<ProjectStatsRowDto> Projects { get; set; } = [];
    public ICollection<ServiceCostAggregateDto> AiUsageBySystem { get; set; } = [];
    public ICollection<PlatformServiceCostAggregateDto> AiUsageByPlatform { get; set; } = [];
    public ICollection<ProjectServiceCostAggregateDto> AiUsageByProject { get; set; } = [];
}
