using System.Text.Json;
using Microsoft.Extensions.Logging;
using SynapseEnqueteManagement.BL.AiCommentSupport;
using SynapseEnqueteManagement.BL.AiIdeaSupport;
using SynapseEnqueteManagement.BL.AiImageDescription;
using SynapseEnqueteManagement.BL.AiModeration;
using SynapseEnqueteManagement.BL.AiQuestionSupport;
using SynapseEnqueteManagement.BL.AiSpeechToText;
using SynapseEnqueteManagement.BL.AiSocialMedia;

namespace SynapseEnqueteManagement.BL.Ai;

/// <summary>
/// Loads <see cref="AiConfig"/> from a JSON file at startup.
///
/// Resolution order (first file found wins):
///   1. Path set in environment variable AI_CONFIG_PATH
///   2. ai-config.json  next to the running assembly
///   3. ai-config.json  in the current working directory
///   4. ai-config.example.json next to the running assembly  (committed fallback)
///   5. ai-config.example.json in the current working directory
///
/// Call <see cref="Load"/> once during startup and register the result as a singleton.
/// </summary>
public static class AiConfigLoader
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling         = JsonCommentHandling.Skip,
        AllowTrailingCommas         = true,
    };

    /// <summary>
    /// Loads the AI configuration. Never returns null — falls back to a safe default
    /// <see cref="AiConfig"/> instance if no file is found, logging a warning.
    /// </summary>
    public static AiConfig Load(ILogger logger)
    {
        foreach (var path in CandidatePaths())
        {
            if (!File.Exists(path)) continue;

            try
            {
                var json   = File.ReadAllText(path);
                var config = JsonSerializer.Deserialize<AiConfig>(json, JsonOpts)
                             ?? throw new InvalidOperationException("Deserialization returned null.");

                logger.LogInformation("[AiConfig] Loaded from {Path}", path);
                return ApplyPlatformSubstitutions(config);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[AiConfig] Failed to parse {Path} — skipping.", path);
            }
        }

        logger.LogWarning(
            "[AiConfig] No ai-config.json or ai-config.example.json found. " +
            "Using built-in defaults. Create ai-config.json to customise prompts.");

        return new AiConfig();
    }

    private static IEnumerable<string> CandidatePaths()
    {
        var envPath = Environment.GetEnvironmentVariable("AI_CONFIG_PATH");
        if (!string.IsNullOrWhiteSpace(envPath))
            yield return envPath;

        var assemblyDir = AppContext.BaseDirectory;
        var workDir     = Directory.GetCurrentDirectory();

        yield return Path.Combine(assemblyDir, "ai-config.json");
        yield return Path.Combine(workDir,     "ai-config.json");

        yield return Path.Combine(assemblyDir, "ai-config.example.json");
        yield return Path.Combine(workDir,     "ai-config.example.json");
    }

    /// <summary>
    /// Replaces {platform_name}, {language}, {country} tokens inside prompt strings
    /// so the JSON stays readable while still being dynamic.
    /// </summary>
    private static AiConfig ApplyPlatformSubstitutions(AiConfig config)
    {
        var p = config.Platform;
        string Sub(string s) => string.IsNullOrEmpty(s) ? s : s
            .Replace("{platform_name}", p.Name,     StringComparison.OrdinalIgnoreCase)
            .Replace("{language}",      p.Language, StringComparison.OrdinalIgnoreCase)
            .Replace("{country}",       p.Country,  StringComparison.OrdinalIgnoreCase);

        // Moderation has no per-request project context, so bake the platform default in at load time.
        string SubWithAge(string s) => string.IsNullOrEmpty(s) ? s : Sub(s)
            .Replace("{age_range}", p.AgeRange, StringComparison.OrdinalIgnoreCase);

        var m = config.Moderation;
        var substitutedModeration = new ModerationConfig
        {
            UseAi                   = m.UseAi,
            DefaultLanguage         = p.Language,
            ModerationPrompt        = SubWithAge(m.ModerationPrompt),
            CommentModerationPrompt = SubWithAge(m.CommentModerationPrompt),
            RewritePrompt           = Sub(m.RewritePrompt),
            ImageModerationPrompt   = SubWithAge(m.ImageModerationPrompt),
            BannedWords             = m.BannedWords,
            RegexPatterns           = m.RegexPatterns,
        };

        // IdeaSupport and CommentSupport substitute {language} per-request (participant's UI language);
        // omit it here so it is not locked to the platform default at startup.
        string SubNoLang(string s) => string.IsNullOrEmpty(s) ? s : s
            .Replace("{platform_name}", p.Name,    StringComparison.OrdinalIgnoreCase)
            .Replace("{country}",       p.Country, StringComparison.OrdinalIgnoreCase);

        var i = config.IdeaSupport;
        var substitutedIdeaSupport = new AiIdeaSupportConfig
        {
            SystemPrompt          = SubNoLang(i.SystemPrompt),   // {language}/{age_range} substituted per-request
            ContextPrompt         = SubNoLang(i.ContextPrompt),  // {language} substituted per-request
            RefinePrompt          = SubNoLang(i.RefinePrompt),   // {language} substituted per-request
            MaxHistoryTurns       = i.MaxHistoryTurns,
            MaxIntroductionLength = i.MaxIntroductionLength,
            DefaultAgeRange       = p.AgeRange,
            FallbackMessage       = i.FallbackMessage,
        };

        var s = config.SpeechToText;
        var substitutedSpeechToText = new AiSpeechToTextConfig
        {
            TranscriptionPrompt = Sub(s.TranscriptionPrompt),
            MaxAudioSizeBytes   = s.MaxAudioSizeBytes,
        };

        var c = config.CommentSupport;
        var substitutedCommentSupport = new AiCommentSupportConfig
        {
            SystemPrompt    = SubNoLang(c.SystemPrompt),   // {language}/{age_range} substituted per-request
            ContextPrompt   = SubNoLang(c.ContextPrompt),  // {language} substituted per-request
            FallbackMessage = c.FallbackMessage,
            DefaultAgeRange = p.AgeRange,
        };

        var q = config.QuestionSupport;
        var substitutedQuestionSupport = new AiQuestionSupportConfig
        {
            SystemPrompt    = SubNoLang(q.SystemPrompt),   // {chat_language}/{question_language} substituted per-request
            ContextPrompt   = SubNoLang(q.ContextPrompt),  // {chat_language}/{question_language} substituted per-request
            FallbackMessage = q.FallbackMessage,
            DefaultAgeRange = p.AgeRange,
        };

        var d = config.ImageDescription;
        var substitutedImageDescription = new AiImageDescriptionConfig
        {
            // {language} is substituted per-request (caller passes the effective language)
            SystemPrompt = SubNoLang(d.SystemPrompt),
        };

        var sm = config.SocialMedia;
        PlatformPostConfig SubPlatform(PlatformPostConfig pc) => new()
        {
            SystemPrompt       = Sub(pc.SystemPrompt),
            UserPromptTemplate = Sub(pc.UserPromptTemplate),
            MaxChars           = pc.MaxChars,
        };
        var substitutedSocialMedia = new SocialMediaConfig
        {
            LinkedIn        = SubPlatform(sm.LinkedIn),
            Facebook        = SubPlatform(sm.Facebook),
            Instagram       = SubPlatform(sm.Instagram),
            FallbackMessage = sm.FallbackMessage,
        };

        return new AiConfig
        {
            Platform        = config.Platform,
            Moderation      = substitutedModeration,
            IdeaSupport     = substitutedIdeaSupport,
            CommentSupport  = substitutedCommentSupport,
            QuestionSupport = substitutedQuestionSupport,
            SpeechToText    = substitutedSpeechToText,
            ImageGeneration = config.ImageGeneration,
            Fuzzy           = config.Fuzzy,
            SocialMedia     = substitutedSocialMedia,
            ImageDescription = substitutedImageDescription,
        };
    }
}
