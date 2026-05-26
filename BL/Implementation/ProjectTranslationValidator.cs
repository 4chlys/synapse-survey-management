using SynapseEnqueteManagement.BL.Domain.I18n.Entities;
using SynapseEnqueteManagement.BL.Exceptions;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.BL.Implementation;

public class ProjectTranslationValidator(ILanguageResolver languageResolver) : IProjectTranslationValidator
{
    public async Task<IReadOnlyDictionary<string, Language>> ValidateAsync(
        ICollection<ProjectTranslationInput> translations,
        ICollection<string> supportedLanguageCodes,
        string defaultLanguageCode,
        ICollection<IntroductionTranslationInput> introductions)
    {
        if (string.IsNullOrWhiteSpace(defaultLanguageCode))
            throw new LocalizedValidationException("Validation.Project.DefaultLanguageCodeRequired");
        if (supportedLanguageCodes.Count == 0)
            throw new LocalizedValidationException("Validation.Project.AtLeastOneSupportedLanguage");
        if (!supportedLanguageCodes.Contains(defaultLanguageCode))
            throw new LocalizedValidationException("Validation.Project.DefaultLanguageMustBeSupported");

        var translationCodes = translations.Select(t => t.LanguageCode).ToList();
        if (translationCodes.Distinct().Count() != translationCodes.Count)
            throw new LocalizedValidationException("Validation.Translation.OneRowPerLanguage");
        if (!supportedLanguageCodes.All(translationCodes.Contains))
            throw new LocalizedValidationException("Validation.Project.AllSupportedLanguagesNeedTranslation");

        if (introductions.GroupBy(i => i.LanguageCode).Any(g => g.Count() > 1))
            throw new LocalizedValidationException("Validation.Project.IntroductionsOneRowPerLanguage");

        var allCodes = supportedLanguageCodes
            .Concat(translationCodes)
            .Concat(introductions.Select(i => i.LanguageCode))
            .Distinct()
            .ToList();

        return await languageResolver.ResolveAsync(allCodes);
    }
}
