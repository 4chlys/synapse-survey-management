using SynapseEnqueteManagement.BL.Domain.I18n.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.BL.Helpers;

// Picks the Language that gets stored on a piece of content (idea/comment/response/reaction).
// The text drives the language, never the author: an explicit submissionLanguageCode wins
// (case-insensitive match in the project's supported set), else the participant's current
// language, else the project's default. Pure — no DB access; the project's SupportedLanguages
// must already be loaded by the caller.
public static class ContentLanguageResolver
{
    public static Language Resolve(Participant participant, Project project, string submissionLanguageCode)
    {
        Language fromSubmission = null;
        if (!string.IsNullOrWhiteSpace(submissionLanguageCode))
            fromSubmission = project?.SupportedLanguages?.FirstOrDefault(
                l => string.Equals(l.Code, submissionLanguageCode, StringComparison.OrdinalIgnoreCase));

        return fromSubmission
               ?? participant?.Language
               ?? participant?.Project?.DefaultLanguage
               ?? project?.DefaultLanguage;
    }
}
