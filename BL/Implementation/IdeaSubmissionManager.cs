using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;
using SynapseEnqueteManagement.BL.Ai;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.BL.Exceptions;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.BL.Implementation;

public class IdeaSubmissionManager(
    IModerationResolver moderationResolver,
    IModerationTokenService moderationTokenService,
    IIdeaManager ideaManager,
    ILogger<IdeaSubmissionManager> logger) : IIdeaSubmissionManager
{
    public async Task<AddIdeaResult> AddModeratedIdeaAsync(
        Project project,
        int categoryId,
        string description,
        string moderationToken,
        string permanentImageUrl,
        string imageModerationToken,
        string languageCode,
        Participant participant,
        ResolvedPlatformAiConfig platformConfig,
        string respondInLanguage = null,
        CancellationToken ct = default)
    {
        var projectId = project?.ProjectId ?? 0;
        var status = await moderationResolver.ResolveStatusAsync(description, moderationToken, projectId, platformConfig, ct, respondInLanguage);

        if (!string.IsNullOrEmpty(permanentImageUrl))
        {
            var imageStatus = string.IsNullOrEmpty(imageModerationToken)
                ? null
                : moderationTokenService.ValidateImageToken(imageModerationToken, projectId);

            if (imageStatus is null)
            {
                logger.LogWarning("Invalid or missing image moderation token for project {ProjectId}", projectId);
                throw new LocalizedValidationException("Validation.Image.InvalidToken");
            }

            // A Flagged image means the AI rejected it but the user chose to submit anyway.
            // Override text status so an admin must review the idea before it becomes visible.
            if (imageStatus == ModerationStatus.Flagged)
                status = ModerationStatus.Flagged;
        }

        string submissionLanguageCode = null;
        if (!string.IsNullOrEmpty(languageCode) && project?.AllowCrossLanguageIdeas == true)
        {
            if (project.SupportedLanguages.Any(l => string.Equals(l.Code, languageCode, StringComparison.OrdinalIgnoreCase)))
                submissionLanguageCode = languageCode;
            else
                throw new ValidationException("Invalid submission language code.");
        }

        return await ideaManager.AddIdeaAsync(categoryId, projectId, description, status, participant,
            permanentImageUrl, submissionLanguageCode, ct);
    }
}
