namespace SynapseEnqueteManagement.API.DTOs.AiUsage;

public record ProjectServiceCostAggregateDto(
    int ProjectId,
    string ShortForm,
    int ProjectPlatformId,
    string ProjectPlatformName,
    string Service,
    string Model,
    long InputTokens,
    long OutputTokens,
    int RequestCount,
    decimal CostEur);
