using SynapseEnqueteManagement.BL.Domain;

namespace SynapseEnqueteManagement.BL.Helpers;

// Centralises the "translation in this language, else first translation" fallback that
// every translation-aware lookup needs (display names, choice labels, branching values).
// Callers project off the picked entity (?.Name, ?.Text, ?.AnswerChoices, ...).
public static class TranslationPicker
{
    public static T Pick<T>(IEnumerable<T> translations, string languageCode)
        where T : class, IHasLanguage
    {
        if (translations is null) return null;
        return translations.FirstOrDefault(t => t.Language?.Code == languageCode)
            ?? translations.FirstOrDefault();
    }
}
