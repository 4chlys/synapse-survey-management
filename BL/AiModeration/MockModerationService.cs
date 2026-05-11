using SynapseEnqueteManagement.BL.Ai;

namespace SynapseEnqueteManagement.BL.AiModeration;

/// <summary>
/// Development/CI stub that requires no API keys.
/// "AI" moderation falls back to the regex layer so the feature still works
/// locally. Rewrite suggestions are not supported and always return null.
/// </summary>
public sealed class MockModerationService(IRegexModerationService regex) : IAiModerationService
{
    public Task<ModerationResult> CheckWithAiAsync(
        string text,
        ResolvedPlatformAiConfig platformConfig,
        CancellationToken ct = default,
        string contentType = "idea",
        string respondInLanguage = null)
        => Task.FromResult(regex.Check(text, contentType));

    public Task<ModerationResult> CheckImageWithAiAsync(byte[] imageBytes, string mimeType, ResolvedPlatformAiConfig platformConfig, CancellationToken ct = default, string respondInLanguage = null)
        => Task.FromResult(ModerationResult.Acceptable());

    public Task<string> SuggestRewriteAsync(string text, ResolvedPlatformAiConfig platformConfig, CancellationToken ct = default, string respondInLanguage = null)
        => Task.FromResult<string>(null);
}