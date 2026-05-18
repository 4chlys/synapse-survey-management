namespace SynapseEnqueteManagement.API.DTOs.AiUsage;

public record ProjectCostAggregateDto(
    int ProjectId,
    string ProjectName,
    int PlatformId,
    string PlatformName,
    long InputTokens,
    long OutputTokens,
    decimal CostEur);
