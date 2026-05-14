using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using SynapseEnqueteManagement.BL.Domain.Ai.Entities;
using SynapseEnqueteManagement.DAL.Interfaces;

namespace SynapseEnqueteManagement.BL.AiUsage;

public class AiUsageManager(
    IAiUsageRepository repository,
    AiPricing pricing,
    ILogger<AiUsageManager> logger) : IAiUsageManager
{
    public async Task LogChatAsync(int projectId, AiServiceKind service, string model, UsageDetails usage, CancellationToken ct = default)
    {
        // projectId <= 0 marks a system-level call (e.g. seed-time embedding backfill
        // or pre-create project image generation); not attributable, so don't bill it.
        if (projectId <= 0) return;

        int inputTokens = 0;
        int outputTokens = 0;
        decimal cost = 0m;

        if (usage == null)
        {
            logger.LogWarning("Usage missing for model {Model}; recording request only, cost=0", model);
        }
        else
        {
            inputTokens = (int)(usage.InputTokenCount ?? 0);
            outputTokens = (int)(usage.OutputTokenCount ?? 0);
            cost = pricing.ComputeChat(model, service, inputTokens, outputTokens);
        }

        await repository.LogAsync(new AiUsageEvent
        {
            Service = service,
            Model = model,
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            RequestCount = 1,
            CostEur = cost,
            CreatedAt = DateTime.UtcNow
        }, projectId);
    }

    public async Task LogImageAsync(int projectId, string model, int count, int width, int height, CancellationToken ct = default)
    {
        if (projectId <= 0) return;

        await repository.LogAsync(new AiUsageEvent
        {
            Service = AiServiceKind.ImageGeneration,
            Model = model,
            InputTokens = 0,
            OutputTokens = 0,
            RequestCount = count,
            CostEur = pricing.ComputeImage(model, count, width, height),
            CreatedAt = DateTime.UtcNow
        }, projectId);
    }

    public async Task LogEmbeddingAsync(int projectId, string model, int inputTokens, CancellationToken ct = default)
    {
        if (projectId <= 0) return;

        await repository.LogAsync(new AiUsageEvent
        {
            Service = AiServiceKind.Embedding,
            Model = model,
            InputTokens = inputTokens,
            OutputTokens = 0,
            RequestCount = 1,
            CostEur = pricing.ComputeEmbedding(model, inputTokens),
            CreatedAt = DateTime.UtcNow
        }, projectId);
    }

    public Task<IEnumerable<ProjectCostAggregate>> ReadProjectAggregatesAsync(DateTime? from, CancellationToken ct = default)
        => repository.ReadProjectAggregatesReadOnlyAsync(from);

    public Task<IEnumerable<PlatformCostAggregate>> ReadPlatformAggregatesAsync(DateTime? from, CancellationToken ct = default)
        => repository.ReadPlatformAggregatesReadOnlyAsync(from);

    public Task<IEnumerable<ServiceCostAggregate>> ReadServiceBreakdownByProjectAsync(int projectId, DateTime? from, CancellationToken ct = default)
        => repository.ReadServiceBreakdownByProjectReadOnlyAsync(projectId, from);
}
