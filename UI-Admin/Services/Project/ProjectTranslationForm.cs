using SynapseEnqueteManagement.UI.Admin.Models.Project;
using SynapseEnqueteManagement.UI.Admin.Models.Section;

namespace SynapseEnqueteManagement.UI.Admin.Services.Project;

// A localized field error the controller turns into a ModelState entry. We keep
// the resource KEY here (not the resolved string) so this stays pure and the
// controller owns IStringLocalizer.
public sealed record TranslationFieldError(string FieldKey, string ResourceKey);

// Result of preparing a submitted project form: the cleaned supported-language
// set, the translations that should actually reach the API (unchecked languages
// dropped), and any per-language validation errors.
public sealed record ProjectTranslationSubmission(
    IReadOnlyList<string> SupportedCodes,
    IReadOnlyList<ProjectTranslationViewModel> ActiveTranslations,
    IReadOnlyList<TranslationFieldError> Errors);

// Pure per-language form logic for the project create/edit page, extracted from
// ProjectController so it can be unit-tested without instantiating the whole
// controller (UserManager, HTTP-backed services, etc.). No state, no I/O.
public static class ProjectTranslationForm
{
    // Languages the admin form lets you toggle. Seeded in DummyDataSeeder.
    // Keep in sync with Create.cshtml's checkbox list.
    public static readonly string[] AvailableLanguageCodes = ["nl", "en"];

    // Picks the admin's current UI language for a new project's default, falling
    // back to the first available language if the culture isn't one we support.
    public static string ResolveDefaultLanguage(string currentCultureTwoLetter)
        => AvailableLanguageCodes.Contains(currentCultureTwoLetter)
            ? currentCultureTwoLetter
            : AvailableLanguageCodes[0];

    // Pads the project's existing translations with empty rows for any available
    // language it doesn't have yet, so the view renders one tab per language and
    // preserves typed data across checkbox toggles.
    public static IList<ProjectTranslationViewModel> BuildTranslations(
        ICollection<ProjectTranslationViewModel> existing)
    {
        var byCode = (existing ?? new List<ProjectTranslationViewModel>())
            .Where(t => !string.IsNullOrEmpty(t.LanguageCode))
            .ToDictionary(t => t.LanguageCode, t => t);
        return AvailableLanguageCodes
            .Select(code => byCode.TryGetValue(code, out var t)
                ? t
                : new ProjectTranslationViewModel { LanguageCode = code })
            .ToList();
    }

    // Same padding for sections: one name input per available language.
    public static IList<SectionInputViewModel> BuildSections(IEnumerable<SectionViewModel> existingSections)
        => existingSections.Select(s => new SectionInputViewModel
        {
            Translations = AvailableLanguageCodes
                .Select(code =>
                {
                    var t = s.Translations?.FirstOrDefault(x => x.LanguageCode == code);
                    return new SectionTranslationViewModel { LanguageCode = code, Name = t?.Name ?? "" };
                })
                .ToList()
        }).ToList();

    // Filters the submitted translations down to currently-supported languages
    // (the form posts hidden inputs for unchecked languages too, so typed data
    // survives toggles, but those must not reach the API) and validates each:
    // Name is always required; Topic and Introduction become required on "next".
    // Field keys carry the index into the ORIGINAL list so they bind back to the
    // right tab's input on re-render.
    public static ProjectTranslationSubmission PrepareSubmission(
        IList<ProjectTranslationViewModel> translations,
        ICollection<string> supportedLanguageCodes,
        bool requireTopicAndIntro)
    {
        var supported = (supportedLanguageCodes ?? new List<string>())
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct()
            .ToList();
        var all = translations ?? new List<ProjectTranslationViewModel>();
        var active = new List<ProjectTranslationViewModel>();
        var errors = new List<TranslationFieldError>();

        for (var i = 0; i < all.Count; i++)
        {
            var t = all[i];
            if (!supported.Contains(t.LanguageCode)) continue;
            active.Add(t);

            if (string.IsNullOrWhiteSpace(t.Name))
                errors.Add(new($"Translations[{i}].Name", "Project_NameRequired"));
            if (requireTopicAndIntro && string.IsNullOrWhiteSpace(t.Topic))
                errors.Add(new($"Translations[{i}].Topic", "Project_TopicRequired"));
            if (requireTopicAndIntro && string.IsNullOrWhiteSpace(t.Introduction))
                errors.Add(new($"Translations[{i}].Introduction", "Project_IntroductionRequired"));
        }

        return new(supported, active, errors);
    }
}
