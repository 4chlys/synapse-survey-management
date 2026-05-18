namespace SynapseEnqueteManagement.UI.Admin.Models.Platform;

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

public class ServiceCostAggregateDto
{
    public string Service { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public long InputTokens { get; set; }
    public long OutputTokens { get; set; }
    public int RequestCount { get; set; }
    public decimal CostEur { get; set; }
}

public class PlatformServiceCostAggregateDto
{
    public int ProjectPlatformId { get; set; }
    public string ProjectPlatformName { get; set; } = string.Empty;
    public string Service { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public long InputTokens { get; set; }
    public long OutputTokens { get; set; }
    public int RequestCount { get; set; }
    public decimal CostEur { get; set; }
}

public class ProjectServiceCostAggregateDto
{
    public int ProjectId { get; set; }
    public string ShortForm { get; set; } = string.Empty;
    public int ProjectPlatformId { get; set; }
    public string ProjectPlatformName { get; set; } = string.Empty;
    public string Service { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public long InputTokens { get; set; }
    public long OutputTokens { get; set; }
    public int RequestCount { get; set; }
    public decimal CostEur { get; set; }
}
