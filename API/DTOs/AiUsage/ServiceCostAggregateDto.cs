namespace SynapseEnqueteManagement.API.DTOs.AiUsage;

public record ServiceCostAggregateDto(
    string Service,
    string Model,
    long InputTokens,
    long OutputTokens,
    decimal CostEur);
