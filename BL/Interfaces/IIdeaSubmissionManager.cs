using SynapseEnqueteManagement.BL.Ai;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.BL.Interfaces;

public interface IIdeaSubmissionManager
{
    Task<AddIdeaResult> AddModeratedIdeaAsync(
        Project project,
        int categoryId,
        string description,
        string moderationToken,
        string tempImageUrl,
        string imageModerationToken,
        string languageCode,
        Participant participant,
        ResolvedPlatformAiConfig platformConfig,
        Func<string, Task<string>> promoteImageAsync,
        string respondInLanguage = null,
        CancellationToken ct = default);
}
