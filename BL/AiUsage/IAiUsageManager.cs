using Microsoft.Extensions.AI;
using SynapseEnqueteManagement.BL.Domain.Ai.Entities;
using SynapseEnqueteManagement.DAL.Interfaces;

namespace SynapseEnqueteManagement.BL.AiUsage;

public interface IAiUsageManager
{
    Task LogChatAsync(int projectId, AiServiceKind service, string model, UsageDetails usage, CancellationToken ct = default);
    Task LogImageAsync(int projectId, string model, int count, int width, int height, CancellationToken ct = default);
    Task LogEmbeddingAsync(int projectId, string model, int inputTokens, CancellationToken ct = default);
    Task<IEnumerable<ProjectCostAggregate>> ReadProjectAggregatesAsync(DateTime? from, CancellationToken ct = default);
    Task<IEnumerable<PlatformCostAggregate>> ReadPlatformAggregatesAsync(DateTime? from, CancellationToken ct = default);
    Task<IEnumerable<ServiceCostAggregate>> ReadServiceBreakdownByProjectAsync(int projectId, DateTime? from, CancellationToken ct = default);
    Task<IEnumerable<ServiceCostAggregate>> ReadServiceBreakdownSystemAsync(int? platformId, DateTime? from, CancellationToken ct = default);
    Task<IEnumerable<PlatformServiceCostAggregate>> ReadServiceBreakdownByPlatformAsync(int? platformId, DateTime? from, CancellationToken ct = default);
    Task<IEnumerable<ProjectServiceCostAggregate>> ReadServiceBreakdownByProjectsAsync(int? platformId, DateTime? from, CancellationToken ct = default);
}
