using SynapseEnqueteManagement.BL.Domain.I18n.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.BL.Helpers;

// Picks the Language that gets stored on a piece of content (idea/comment/response/reaction).
// The text drives the language, never the author: an explicit submissionLanguageCode wins
// (case-insensitive match in the project's supported set), else the participant's current
// language (only when the project supports it), else the project's default. Pure — no DB
// access; the project's SupportedLanguages must already be loaded by the caller.
public static class ContentLanguageResolver
{
    public static Language Resolve(Participant participant, Project project, string submissionLanguageCode)
    {
        var supported = project?.SupportedLanguages;

        Language fromSubmission = null;
        if (!string.IsNullOrWhiteSpace(submissionLanguageCode))
            fromSubmission = supported?.FirstOrDefault(
                l => string.Equals(l.Code, submissionLanguageCode, StringComparison.OrdinalIgnoreCase));

        // Clamp the participant's language to the supported set when one is loaded; otherwise keep it.
        var participantLanguage = participant?.Language;
        if (participantLanguage != null && supported != null && supported.Any()
            && !supported.Any(l => string.Equals(l.Code, participantLanguage.Code, StringComparison.OrdinalIgnoreCase)))
            participantLanguage = null;

        return fromSubmission
               ?? participantLanguage
               ?? participant?.Project?.DefaultLanguage
               ?? project?.DefaultLanguage;
    }
}
