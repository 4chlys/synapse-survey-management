using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace SynapseEnqueteManagement.BL.AiSocialMedia;

public sealed class GoogleVertexAiSocialPostGenerator(
    IChatClient chatClient,
    SocialMediaConfig config,
    ILogger<GoogleVertexAiSocialPostGenerator> logger)
    : ISocialPostGenerator
{
    private static readonly Regex CodeFenceRe =
        new(@"^```[a-zA-Z]*\s*|\s*```$", RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<SocialPostReply> GenerateAsync(SocialPostRequest request, CancellationToken ct = default)
    {
        var platformConfig = request.Platform switch
        {
            SocialPlatform.LinkedIn  => config.LinkedIn,
            SocialPlatform.Facebook  => config.Facebook,
            SocialPlatform.Instagram => config.Instagram,
            _ => config.LinkedIn,
        };

        try
        {
            var topIdeasBlock      = string.Join(", ", request.Stats.TopIdeaDescriptions);
            var topCategoriesBlock = string.Join(", ", request.Stats.TopCategoryNames);
            var completionPct      = request.Stats.CompletionRate.ToString("F1");

            var userPrompt = platformConfig.UserPromptTemplate
                .Replace("{project_name}",       request.ProjectName,      StringComparison.OrdinalIgnoreCase)
                .Replace("{organisation_name}",  request.OrganisationName, StringComparison.OrdinalIgnoreCase)
                .Replace("{project_topic}",      request.ProjectTopic,     StringComparison.OrdinalIgnoreCase)
                .Replace("{project_introduction}", request.ProjectIntroduction, StringComparison.OrdinalIgnoreCase)
                .Replace("{user_description}",   request.UserDescription,  StringComparison.OrdinalIgnoreCase)
                .Replace("{view_count}",         request.Stats.ViewCount.ToString(),       StringComparison.OrdinalIgnoreCase)
                .Replace("{completion_count}",   request.Stats.CompletionCount.ToString(), StringComparison.OrdinalIgnoreCase)
                .Replace("{completion_pct}",     completionPct,            StringComparison.OrdinalIgnoreCase)
                .Replace("{top_ideas_block}",    topIdeasBlock,            StringComparison.OrdinalIgnoreCase)
                .Replace("{top_categories_block}", topCategoriesBlock,     StringComparison.OrdinalIgnoreCase)
                .Replace("{max_chars}",          platformConfig.MaxChars.ToString(), StringComparison.OrdinalIgnoreCase);

            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, platformConfig.SystemPrompt),
                new(ChatRole.User,   userPrompt),
            };

            var response = await chatClient.GetResponseAsync(messages, cancellationToken: ct);
            var raw      = response.Messages.LastOrDefault()?.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(raw))
                return Fallback(platformConfig);

            return ParseReply(raw, platformConfig);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[SocialPost] Generate call failed for platform {Platform}", request.Platform);
            return Fallback(platformConfig);
        }
    }

    private SocialPostReply ParseReply(string raw, PlatformPostConfig platformConfig)
    {
        var cleaned = CodeFenceRe.Replace(raw, string.Empty).Trim();
        var start   = cleaned.IndexOf('{');
        var end     = cleaned.LastIndexOf('}');
        var json    = start >= 0 && end > start ? cleaned[start..(end + 1)] : cleaned;

        try
        {
            var aiReply = JsonSerializer.Deserialize<SocialPostAiReply>(json, JsonOpts);
            if (aiReply is null || string.IsNullOrWhiteSpace(aiReply.Copy))
                return Fallback(platformConfig);

            var copy = aiReply.Copy.Length > platformConfig.MaxChars
                ? aiReply.Copy[..platformConfig.MaxChars]
                : aiReply.Copy;

            return new SocialPostReply(copy, aiReply.Hashtags ?? [], copy.Length);
        }
        catch (JsonException)
        {
            return new SocialPostReply(raw, [], raw.Length);
        }
    }

    private SocialPostReply Fallback(PlatformPostConfig _) =>
        new(config.FallbackMessage, [], config.FallbackMessage.Length);

    private record SocialPostAiReply(
        [property: JsonPropertyName("copy")]     string       Copy,
        [property: JsonPropertyName("hashtags")] List<string> Hashtags);
}
