using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using SynapseEnqueteManagement.BL.Ai;
using SynapseEnqueteManagement.BL.AiSupport;
using SynapseEnqueteManagement.BL.AiUsage;
using SynapseEnqueteManagement.BL.Domain.Ai.Entities;

namespace SynapseEnqueteManagement.BL.AiSocialMedia;

public sealed class AiSocialPostService(
    SocialMediaConfig config,
    ILogger<AiSocialPostService> logger,
    IAiUsageManager usageManager)
    : ChatClientAiSupportBase<SocialPostRequest, SocialPostReply>(logger, usageManager),
      IAiSocialPostService
{
    protected override AiServiceKind Service => AiServiceKind.SocialPost;

    protected override List<ChatMessage> BuildMessages(SocialPostRequest request, ResolvedPlatformAiConfig platformConfig)
    {
        var platformPostConfig = request.Platform switch
        {
            SocialPlatform.LinkedIn  => config.LinkedIn,
            SocialPlatform.Facebook  => config.Facebook,
            SocialPlatform.Instagram => config.Instagram,
            _ => config.LinkedIn,
        };

        var topIdeasBlock      = string.Join(", ", request.Stats.TopIdeaDescriptions);
        var topCategoriesBlock = string.Join(", ", request.Stats.TopCategoryNames);
        var completionPct      = request.Stats.CompletionRate.ToString("F1");

        var userPrompt = platformPostConfig.UserPromptTemplate
            .Replace("{project_name}",         request.ProjectName,                          StringComparison.OrdinalIgnoreCase)
            .Replace("{organisation_name}",    request.OrganisationName,                     StringComparison.OrdinalIgnoreCase)
            .Replace("{project_topic}",        request.ProjectTopic,                         StringComparison.OrdinalIgnoreCase)
            .Replace("{project_introduction}", request.ProjectIntroduction,                  StringComparison.OrdinalIgnoreCase)
            .Replace("{user_description}",     request.UserDescription,                      StringComparison.OrdinalIgnoreCase)
            .Replace("{view_count}",           request.Stats.ViewCount.ToString(),           StringComparison.OrdinalIgnoreCase)
            .Replace("{completion_count}",     request.Stats.CompletionCount.ToString(),     StringComparison.OrdinalIgnoreCase)
            .Replace("{completion_pct}",       completionPct,                                StringComparison.OrdinalIgnoreCase)
            .Replace("{top_ideas_block}",      topIdeasBlock,                                StringComparison.OrdinalIgnoreCase)
            .Replace("{top_categories_block}", topCategoriesBlock,                           StringComparison.OrdinalIgnoreCase)
            .Replace("{max_chars}",            platformPostConfig.MaxChars.ToString(),       StringComparison.OrdinalIgnoreCase);

        return new List<ChatMessage>
        {
            new(ChatRole.System, platformPostConfig.SystemPrompt),
            new(ChatRole.User,   userPrompt),
        };
    }

    protected override SocialPostReply ParseRawReply(string raw, SocialPostRequest request, ResolvedPlatformAiConfig platformConfig)
    {
        var platformPostConfig = request.Platform switch
        {
            SocialPlatform.LinkedIn  => config.LinkedIn,
            SocialPlatform.Facebook  => config.Facebook,
            SocialPlatform.Instagram => config.Instagram,
            _ => config.LinkedIn,
        };

        var cleaned = StripCodeFences(raw);
        var json    = ExtractJson(cleaned);

        try
        {
            var aiReply = JsonSerializer.Deserialize<SocialPostAiReply>(json, JsonOpts);
            if (aiReply is null || string.IsNullOrWhiteSpace(aiReply.Copy))
                return FallbackReply(platformConfig);

            var copy = aiReply.Copy.Length > platformPostConfig.MaxChars
                ? aiReply.Copy[..platformPostConfig.MaxChars]
                : aiReply.Copy;

            return new SocialPostReply(copy, aiReply.Hashtags ?? [], copy.Length);
        }
        catch (JsonException)
        {
            return new SocialPostReply(raw, [], raw.Length);
        }
    }

    protected override SocialPostReply FallbackReply(ResolvedPlatformAiConfig platformConfig) =>
        new(config.FallbackMessage, [], config.FallbackMessage.Length);

    private record SocialPostAiReply(
        [property: JsonPropertyName("copy")]     string       Copy,
        [property: JsonPropertyName("hashtags")] List<string> Hashtags);
}
