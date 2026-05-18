using SynapseEnqueteManagement.BL.Ai;

namespace SynapseEnqueteManagement.BL.AiModeration;

public sealed class MockModerationService(IRegexModerationService regex) : IAiModerationService
{
    public Task<ModerationResult> CheckWithAiAsync(
        string text,
        ResolvedPlatformAiConfig platformConfig,
        int projectId,
        CancellationToken ct = default,
        string contentType = "idea",
        string respondInLanguage = null)
        => Task.FromResult(regex.Check(text, contentType));

    public Task<ModerationResult> CheckImageWithAiAsync(byte[] imageBytes, string mimeType, ResolvedPlatformAiConfig platformConfig, int projectId, CancellationToken ct = default, string respondInLanguage = null)
        => Task.FromResult(ModerationResult.Acceptable());

    public Task<string> SuggestRewriteAsync(string text, ResolvedPlatformAiConfig platformConfig, int projectId, CancellationToken ct = default, string respondInLanguage = null)
        => Task.FromResult<string>(null);
}