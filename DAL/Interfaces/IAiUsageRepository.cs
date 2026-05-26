using SynapseEnqueteManagement.BL.Domain.Ai.Entities;

namespace SynapseEnqueteManagement.DAL.Interfaces;

public interface IAiUsageRepository : IRepository<AiUsageEvent>
{
    Task LogAsync(AiUsageEvent ev, int projectId, CancellationToken ct = default);

    Task<IEnumerable<ProjectCostAggregate>> ReadProjectAggregatesReadOnlyAsync(DateTime? from);

    Task<IEnumerable<PlatformCostAggregate>> ReadPlatformAggregatesReadOnlyAsync(DateTime? from);

    Task<IEnumerable<ServiceCostAggregate>> ReadServiceBreakdownByProjectReadOnlyAsync(int projectId, DateTime? from);

    Task<IEnumerable<ServiceCostAggregate>> ReadServiceBreakdownSystemReadOnlyAsync(int? platformId, DateTime? from);

    Task<IEnumerable<PlatformServiceCostAggregate>> ReadServiceBreakdownByPlatformReadOnlyAsync(int? platformId, DateTime? from);

    Task<IEnumerable<ProjectServiceCostAggregate>> ReadServiceBreakdownByProjectsReadOnlyAsync(int? platformId, DateTime? from);
}

public record ProjectCostAggregate(
    int ProjectId,
    string ShortForm,
    int ProjectPlatformId,
    string ProjectPlatformName,
    long InputTokens,
    long OutputTokens,
    int RequestCount,
    decimal CostEur);

public record PlatformCostAggregate(
    int ProjectPlatformId,
    string ProjectPlatformName,
    long InputTokens,
    long OutputTokens,
    int RequestCount,
    decimal CostEur);

public record ServiceCostAggregate(
    AiServiceKind Service,
    string Model,
    long InputTokens,
    long OutputTokens,
    int RequestCount,
    decimal CostEur);

public sealed record PlatformServiceCostAggregate(
    int ProjectPlatformId,
    string ProjectPlatformName,
    AiServiceKind Service,
    string Model,
    long InputTokens,
    long OutputTokens,
    int RequestCount,
    decimal CostEur);

public sealed record ProjectServiceCostAggregate(
    int ProjectId,
    string ShortForm,
    int ProjectPlatformId,
    string ProjectPlatformName,
    AiServiceKind Service,
    string Model,
    long InputTokens,
    long OutputTokens,
    int RequestCount,
    decimal CostEur);
