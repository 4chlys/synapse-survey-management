using System.Collections.Concurrent;
using Microsoft.Extensions.AI;
using SynapseEnqueteManagement.BL.Domain.Ai;

namespace SynapseEnqueteManagement.API.Ai;

public sealed class ModelAvailabilityService(
    ChatClientRegistry registry,
    ILogger<ModelAvailabilityService> logger)
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IReadOnlyDictionary<string, bool> _cache;
    private DateTime _cacheExpiry = DateTime.MinValue;

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan ProbeTimeout   = TimeSpan.FromSeconds(10);

    public async Task<IReadOnlyDictionary<string, bool>> GetAvailabilityAsync(CancellationToken ct = default)
    {
        if (_cache is not null && DateTime.UtcNow < _cacheExpiry)
            return _cache;

        await _lock.WaitAsync(ct);
        try
        {
            if (_cache is not null && DateTime.UtcNow < _cacheExpiry)
                return _cache;

            _cache      = await ProbeAllAsync(ct);
            _cacheExpiry = DateTime.UtcNow + CacheDuration;
            return _cache;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<IReadOnlyDictionary<string, bool>> ProbeAllAsync(CancellationToken ct)
    {
        logger.LogInformation("[AI] Probing regional availability for {Count} model(s)...", registry.AvailableModelIds.Count);

        var results = new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        var vertexIds = AiModelCatalog.All
            .Where(m => m.Provider == AiProvider.VertexAi)
            .Select(m => m.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        await Task.WhenAll(registry.AvailableModelIds.Select(async modelId =>
        {
            if (!registry.TryGet(modelId, out var client))
            {
                results[modelId] = false;
                return;
            }

            // Only Vertex AI models have regional availability constraints; for other providers a
            // registered client (API key present) is sufficient — avoid a billable probe per provider.
            if (!vertexIds.Contains(modelId))
            {
                results[modelId] = true;
                return;
            }

            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(ProbeTimeout);

                await client.GetResponseAsync(
                    [new ChatMessage(ChatRole.User, "Hi")],
                    new ChatOptions { MaxOutputTokens = 1 },
                    cts.Token);

                results[modelId] = true;
                logger.LogDebug("[AI] Model {ModelId}: available", modelId);
            }
            catch (Exception ex)
            {
                results[modelId] = false;
                logger.LogInformation("[AI] Model {ModelId}: unavailable — {Error}", modelId, ex.Message);
            }
        }));

        var available   = results.Count(r => r.Value);
        var unavailable = results.Count - available;
        logger.LogInformation("[AI] Availability probe complete: {Available} available, {Unavailable} unavailable", available, unavailable);

        return results;
    }
}
