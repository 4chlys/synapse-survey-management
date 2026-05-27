using SynapseEnqueteManagement.BL.Domain;

namespace SynapseEnqueteManagement.BL.Helpers;

// Centralises the "translation in this language, else fall back" lookup that every
// translation-aware projection needs (display names, choice labels, branching values).
// Callers project off the picked entity (?.Name, ?.Text, ?.AnswerChoices, ...).
public static class TranslationPicker
{
    // 2-tier: requested → first available. Use when there is no project-default context.
    public static T Pick<T>(IEnumerable<T> translations, string languageCode)
        where T : class, IHasLanguage
    {
        if (translations is null) return null;
        return translations.FirstOrDefault(t => t.Language?.Code == languageCode)
            ?? translations.FirstOrDefault();
    }

    // 3-tier: requested → project default → first available. Use when the caller knows
    // the project's default language and wants stat/export output to land there if the
    // requested language is missing (also covers UI-culture + project-default scenarios
    // like social-post generation). The materialisation prevents triple enumeration when
    // callers pass a LINQ chain.
    public static T Pick<T>(IEnumerable<T> translations, string languageCode, string defaultLanguageCode)
        where T : class, IHasLanguage
    {
        if (translations is null) return null;
        var list = translations as IList<T> ?? translations.ToList();
        return list.FirstOrDefault(t => t.Language?.Code == languageCode)
            ?? list.FirstOrDefault(t => t.Language?.Code == defaultLanguageCode)
            ?? list.FirstOrDefault();
    }
}
