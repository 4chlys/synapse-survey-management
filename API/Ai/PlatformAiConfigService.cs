using System.Data.Common;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SynapseEnqueteManagement.API.Data;
using SynapseEnqueteManagement.BL.Ai;
using SynapseEnqueteManagement.BL.AiCommentSupport;
using SynapseEnqueteManagement.BL.AiIdeaSupport;
using SynapseEnqueteManagement.BL.AiModeration;
using SynapseEnqueteManagement.BL.AiQuestionSupport;
using SynapseEnqueteManagement.BL.Domain.Ai;

namespace SynapseEnqueteManagement.API.Ai;

public class PlatformAiConfigService(
    AdminReadContext adminDb,
    AiConfig globalConfig,
    ChatClientRegistry registry,
    IMemoryCache cache,
    ILogger<PlatformAiConfigService> logger) : IPlatformAiConfigService
{
    // Short TTL so UI-Admin prompt/model changes are picked up quickly.
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

    public async Task<ResolvedPlatformAiConfig> ResolveAsync(int platformId, CancellationToken ct)
    {
        var cacheKey = $"platform-ai:{platformId}";
        if (cache.TryGetValue(cacheKey, out ResolvedPlatformAiConfig cached))
            return cached;

        SystemAiSettingsReadModel settings = null;
        PlatformSettingsReadModel platform = null;
        try
        {
            settings = await adminDb.SystemAiSettings.FirstOrDefaultAsync(ct);
            platform = await adminDb.PlatformSettings
                .FirstOrDefaultAsync(s => s.ProjectPlatformId == platformId, ct);
        }
        catch (DbException ex)
        {
            logger.LogWarning(ex,
                "[AI] Admin DB unavailable for platform {PlatformId}; using global defaults.", platformId);
        }

        var resolved = Build(settings, platform);
        cache.Set(cacheKey, resolved, CacheTtl);
        return resolved;
    }

    public async Task<ResolvedPlatformAiConfig> ResolveGlobalAsync(CancellationToken ct)
    {
        const string cacheKey = "platform-ai:global";
        if (cache.TryGetValue(cacheKey, out ResolvedPlatformAiConfig cached))
            return cached;

        SystemAiSettingsReadModel settings = null;
        try
        {
            settings = await adminDb.SystemAiSettings.FirstOrDefaultAsync(ct);
        }
        catch (DbException ex)
        {
            logger.LogWarning(ex, "[AI] Admin DB unavailable; using global defaults.");
        }

        var resolved = Build(settings, platform: null);
        cache.Set(cacheKey, resolved, CacheTtl);
        return resolved;
    }

    public async Task<IdeaSummaryAiConfig> ResolveIdeaSummaryAsync(int platformId, CancellationToken ct)
    {
        var cacheKey = $"platform-ai-summary:{platformId}";
        if (cache.TryGetValue(cacheKey, out IdeaSummaryAiConfig cached))
            return cached;

        SystemAiSettingsReadModel settings = null;
        PlatformSettingsReadModel platform = null;
        try
        {
            settings = await adminDb.SystemAiSettings.FirstOrDefaultAsync(ct);
            platform = await adminDb.PlatformSettings
                .FirstOrDefaultAsync(s => s.ProjectPlatformId == platformId, ct);
        }
        catch (DbException ex)
        {
            logger.LogWarning(ex,
                "[AI] Admin DB unavailable for summary config on platform {PlatformId}.", platformId);
        }

        var resolved = Build(settings, platform);
        var prompt = !string.IsNullOrWhiteSpace(platform?.PromptIdeaSummary)
            ? platform.PromptIdeaSummary
            : globalConfig.IdeaSummary.SystemPrompt;
        var maxIdeas = platform?.IdeaSummaryMaxIdeas is > 0 ? platform.IdeaSummaryMaxIdeas : 200;
        var result = new IdeaSummaryAiConfig(resolved.ChatClient, resolved.SelectedModelId, prompt, maxIdeas);
        cache.Set(cacheKey, result, CacheTtl);
        return result;
    }

    private ResolvedPlatformAiConfig Build(SystemAiSettingsReadModel settings, PlatformSettingsReadModel platform)
    {
        var allowedIds = settings is not null
            ? JsonSerializer.Deserialize<List<string>>(settings.AllowedModelIds) ?? []
            : AiModelCatalog.All.Where(m => m.Provider == AiProvider.VertexAi).Select(m => m.Id).ToList();

        var selectedModel = platform?.SelectedAiModel ?? registry.DefaultModelId;
        var effectiveModel = allowedIds.Contains(selectedModel ?? string.Empty)
            ? selectedModel
            : allowedIds.FirstOrDefault(id => registry.TryGet(id, out _));

        IChatClient chatClient;
        string actualModelId;

        if (effectiveModel is not null && registry.TryGet(effectiveModel, out chatClient))
        {
            actualModelId = effectiveModel;
        }
        else
        {
            var fallbackId = registry.AvailableModelIds.FirstOrDefault();
            if (fallbackId is null || !registry.TryGet(fallbackId, out chatClient))
                throw new InvalidOperationException(
                    "No AI model is available. Verify that at least one provider API key is configured.");
            actualModelId = fallbackId;
        }

        var m = globalConfig.Moderation;
        var moderation = new ModerationConfig
        {
            UseAi                   = m.UseAi,
            DefaultLanguage         = m.DefaultLanguage,
            ModerationPrompt        = platform?.PromptModeration        ?? m.ModerationPrompt,
            CommentModerationPrompt = platform?.PromptCommentModeration ?? m.CommentModerationPrompt,
            RewritePrompt           = platform?.PromptRewrite           ?? m.RewritePrompt,
            ImageModerationPrompt   = platform?.PromptImageModeration   ?? m.ImageModerationPrompt,
            BannedWords             = m.BannedWords,
            RegexPatterns           = m.RegexPatterns,
        };

        var i = globalConfig.IdeaSupport;
        var ideaSupport = new AiIdeaSupportConfig
        {
            SystemPrompt          = platform?.PromptIdeaSupport ?? i.SystemPrompt,
            ContextPrompt         = i.ContextPrompt,
            RefinePrompt          = i.RefinePrompt,
            MaxHistoryTurns       = i.MaxHistoryTurns,
            MaxIntroductionLength = i.MaxIntroductionLength,
            DefaultAgeRange       = i.DefaultAgeRange,
            FallbackMessage       = i.FallbackMessage,
        };

        var c = globalConfig.CommentSupport;
        var commentSupport = new AiCommentSupportConfig
        {
            SystemPrompt    = platform?.PromptCommentSupport ?? c.SystemPrompt,
            ContextPrompt   = c.ContextPrompt,
            FallbackMessage = c.FallbackMessage,
            DefaultAgeRange = c.DefaultAgeRange,
        };

        var q = globalConfig.QuestionSupport;
        var questionSupport = new AiQuestionSupportConfig
        {
            SystemPrompt    = platform?.PromptQuestionSupport ?? q.SystemPrompt,
            ContextPrompt   = q.ContextPrompt,
            FallbackMessage = q.FallbackMessage,
            DefaultAgeRange = q.DefaultAgeRange,
        };

        return new ResolvedPlatformAiConfig(chatClient, actualModelId, moderation, ideaSupport, commentSupport, questionSupport);
    }
}
