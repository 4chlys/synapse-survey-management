using SynapseEnqueteManagement.BL.Ai;

namespace SynapseEnqueteManagement.BL.AiModeration;

public interface IAiModerationService
{
    Task<ModerationResult> CheckWithAiAsync(string text, ResolvedPlatformAiConfig platformConfig, int projectId, CancellationToken ct = default, string contentType = "idea", string respondInLanguage = null);
    Task<ModerationResult> CheckImageWithAiAsync(byte[] imageBytes, string mimeType, ResolvedPlatformAiConfig platformConfig, int projectId, CancellationToken ct = default, string respondInLanguage = null);
    Task<string> SuggestRewriteAsync(string text, ResolvedPlatformAiConfig platformConfig, int projectId, CancellationToken ct = default, string respondInLanguage = null);
}