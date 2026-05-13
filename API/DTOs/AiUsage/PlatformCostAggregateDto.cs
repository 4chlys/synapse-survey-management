namespace SynapseEnqueteManagement.API.DTOs.AiUsage;

public record PlatformCostAggregateDto(
    int PlatformId,
    string PlatformName,
    long InputTokens,
    long OutputTokens,
    decimal CostEur);
