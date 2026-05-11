using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
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
    IMemoryCache cache) : IPlatformAiConfigService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public async Task<ResolvedPlatformAiConfig> ResolveAsync(int platformId, CancellationToken ct)
    {
        var cacheKey = $"platform-ai:{platformId}";
        if (cache.TryGetValue(cacheKey, out ResolvedPlatformAiConfig cached))
            return cached;

        var settings = await adminDb.SystemAiSettings.FirstOrDefaultAsync(ct);
        var platform  = await adminDb.PlatformSettings
            .FirstOrDefaultAsync(s => s.ProjectPlatformId == platformId, ct);

        var resolved = Build(settings, platform);
        cache.Set(cacheKey, resolved, CacheTtl);
        return resolved;
    }

    public async Task<ResolvedPlatformAiConfig> ResolveGlobalAsync(CancellationToken ct)
    {
        const string cacheKey = "platform-ai:global";
        if (cache.TryGetValue(cacheKey, out ResolvedPlatformAiConfig cached))
            return cached;

        var settings = await adminDb.SystemAiSettings.FirstOrDefaultAsync(ct);
        var resolved = Build(settings, platform: null);
        cache.Set(cacheKey, resolved, CacheTtl);
        return resolved;
    }

    private ResolvedPlatformAiConfig Build(SystemAiSettingsReadModel settings, PlatformSettingsReadModel platform)
    {
        var allowedIds = settings is not null
            ? JsonSerializer.Deserialize<List<string>>(settings.AllowedModelIds) ?? []
            : AiModelCatalog.All.Where(m => m.Provider == AiProvider.VertexAi).Select(m => m.Id).ToList();

        var selectedModel = platform?.SelectedAiModel;
        var effectiveModel = allowedIds.Contains(selectedModel ?? string.Empty)
            ? selectedModel
            : allowedIds.FirstOrDefault(id => registry.TryGet(id, out _));

        if (effectiveModel is null || !registry.TryGet(effectiveModel, out var chatClient))
            throw new InvalidOperationException("No available AI model found. Check ChatClientRegistry and SystemAiSettings.");

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

        return new ResolvedPlatformAiConfig(chatClient, moderation, ideaSupport, commentSupport, questionSupport);
    }
}
