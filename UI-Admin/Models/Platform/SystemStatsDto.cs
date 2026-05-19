namespace SynapseEnqueteManagement.UI.Admin.Models.Platform;

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

public record ServiceCostAggregateDto
{
    public string Service { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public long InputTokens { get; init; }
    public long OutputTokens { get; init; }
    public int RequestCount { get; init; }
    public decimal CostEur { get; init; }
}

public record PlatformServiceCostAggregateDto
{
    public int ProjectPlatformId { get; init; }
    public string ProjectPlatformName { get; init; } = string.Empty;
    public string Service { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public long InputTokens { get; init; }
    public long OutputTokens { get; init; }
    public int RequestCount { get; init; }
    public decimal CostEur { get; init; }
}

public record ProjectServiceCostAggregateDto
{
    public int ProjectId { get; init; }
    public string ShortForm { get; init; } = string.Empty;
    public int ProjectPlatformId { get; init; }
    public string ProjectPlatformName { get; init; } = string.Empty;
    public string Service { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public long InputTokens { get; init; }
    public long OutputTokens { get; init; }
    public int RequestCount { get; init; }
    public decimal CostEur { get; init; }
}
