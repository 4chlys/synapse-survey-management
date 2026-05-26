using SynapseEnqueteManagement.BL.Domain.I18n.Entities;

namespace SynapseEnqueteManagement.BL.Interfaces;

// Multi-step validator for a project's translation set + introductions, shared by Create/Update/
// Duplicate flows on ProjectManager. Returns the resolved {code → Language} map so callers can
// attach the tracked entities to the project graph without re-querying.
public interface IProjectTranslationValidator
{
    Task<IReadOnlyDictionary<string, Language>> ValidateAsync(
        ICollection<ProjectTranslationInput> translations,
        ICollection<string> supportedLanguageCodes,
        string defaultLanguageCode,
        ICollection<IntroductionTranslationInput> introductions);
}
