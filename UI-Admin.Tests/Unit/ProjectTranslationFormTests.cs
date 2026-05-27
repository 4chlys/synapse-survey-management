using SynapseEnqueteManagement.UI.Admin.Models.Project;
using SynapseEnqueteManagement.UI.Admin.Models.Section;
using SynapseEnqueteManagement.UI.Admin.Services.Project;

namespace SynapseEnqueteManagement.UI.Admin.Tests.Unit;

// Pure per-language form logic extracted from ProjectController so it can be
// tested without instantiating the controller (UserManager, HTTP services).
public class ProjectTranslationFormTests
{
    #region ResolveDefaultLanguage

    [Theory]
    [InlineData("nl", "nl")]
    [InlineData("en", "en")]
    public void ResolveDefaultLanguage_SupportedCulture_ReturnsItUnchanged(string culture, string expected)
        => Assert.Equal(expected, ProjectTranslationForm.ResolveDefaultLanguage(culture));

    [Theory]
    [InlineData("fr")]
    [InlineData("de")]
    [InlineData("")]
    public void ResolveDefaultLanguage_UnsupportedCulture_FallsBackToFirstAvailable(string culture)
        => Assert.Equal(ProjectTranslationForm.AvailableLanguageCodes[0],
            ProjectTranslationForm.ResolveDefaultLanguage(culture));

    #endregion

    #region BuildTranslations

    [Fact]
    public void BuildTranslations_Null_ReturnsOneEmptyRowPerLanguageInOrder()
    {
        var result = ProjectTranslationForm.BuildTranslations(null);

        Assert.Equal(new[] { "nl", "en" }, result.Select(t => t.LanguageCode));
        Assert.All(result, t => Assert.True(string.IsNullOrEmpty(t.Name)));
    }

    [Fact]
    public void BuildTranslations_PartialExisting_PreservesKnownAndPadsMissing()
    {
        var existing = new List<ProjectTranslationViewModel>
        {
            new() { LanguageCode = "en", Name = "Name EN", Topic = "Topic EN" },
        };

        var result = ProjectTranslationForm.BuildTranslations(existing);

        var nl = result.Single(t => t.LanguageCode == "nl");
        var en = result.Single(t => t.LanguageCode == "en");
        Assert.True(string.IsNullOrEmpty(nl.Name));
        Assert.Equal("Name EN", en.Name);
        // Order still follows AvailableLanguageCodes regardless of input order.
        Assert.Equal(new[] { "nl", "en" }, result.Select(t => t.LanguageCode));
    }

    [Fact]
    public void BuildTranslations_IgnoresEntriesWithBlankLanguageCode()
    {
        var existing = new List<ProjectTranslationViewModel>
        {
            new() { LanguageCode = "", Name = "Orphan" },
            new() { LanguageCode = "nl", Name = "Naam" },
        };

        var result = ProjectTranslationForm.BuildTranslations(existing);

        Assert.Equal(2, result.Count);
        Assert.Equal("Naam", result.Single(t => t.LanguageCode == "nl").Name);
        Assert.DoesNotContain(result, t => t.Name == "Orphan");
    }

    #endregion

    #region BuildSections

    [Fact]
    public void BuildSections_FillsNameFromExisting_AndPadsMissingLanguageWithEmpty()
    {
        var sections = new List<SectionViewModel>
        {
            new()
            {
                SectionId = 1,
                Translations = new List<SectionTranslationViewModel>
                {
                    new() { LanguageCode = "nl", Name = "Sectie NL" },
                    // en missing
                },
            },
        };

        var result = ProjectTranslationForm.BuildSections(sections);

        var row = Assert.Single(result);
        Assert.Equal(new[] { "nl", "en" }, row.Translations.Select(t => t.LanguageCode));
        Assert.Equal("Sectie NL", row.Translations.Single(t => t.LanguageCode == "nl").Name);
        Assert.Equal("", row.Translations.Single(t => t.LanguageCode == "en").Name);
    }

    #endregion

    #region PrepareSubmission

    private static List<ProjectTranslationViewModel> Bilingual(
        string nlName = "Naam", string enName = "Name") => new()
    {
        new() { LanguageCode = "nl", Name = nlName, Topic = "Onderwerp", Introduction = "Intro" },
        new() { LanguageCode = "en", Name = enName, Topic = "Topic", Introduction = "Intro" },
    };

    [Fact]
    public void PrepareSubmission_AllSupportedAndFilled_NoErrors_BothActive()
    {
        var result = ProjectTranslationForm.PrepareSubmission(
            Bilingual(), new[] { "nl", "en" }, requireTopicAndIntro: true);

        Assert.Empty(result.Errors);
        Assert.Equal(2, result.ActiveTranslations.Count);
        Assert.Equal(new[] { "nl", "en" }, result.SupportedCodes);
    }

    [Fact]
    public void PrepareSubmission_UncheckedLanguage_ExcludedAndNotValidated()
    {
        // English is fully blank but NOT supported → must be dropped, no error.
        var translations = new List<ProjectTranslationViewModel>
        {
            new() { LanguageCode = "nl", Name = "Naam", Topic = "Onderwerp", Introduction = "Intro" },
            new() { LanguageCode = "en", Name = "", Topic = "", Introduction = "" },
        };

        var result = ProjectTranslationForm.PrepareSubmission(
            translations, new[] { "nl" }, requireTopicAndIntro: true);

        Assert.Empty(result.Errors);
        Assert.Single(result.ActiveTranslations);
        Assert.Equal("nl", result.ActiveTranslations[0].LanguageCode);
    }

    [Fact]
    public void PrepareSubmission_MissingName_AlwaysErrors_EvenWhenNotRequiringFull()
    {
        var translations = Bilingual(enName: "");

        var result = ProjectTranslationForm.PrepareSubmission(
            translations, new[] { "nl", "en" }, requireTopicAndIntro: false);

        var error = Assert.Single(result.Errors);
        Assert.Equal("Translations[1].Name", error.FieldKey);
        Assert.Equal("Project_NameRequired", error.ResourceKey);
    }

    [Fact]
    public void PrepareSubmission_RequireFull_BlankTopicAndIntro_ProduceErrors()
    {
        var translations = new List<ProjectTranslationViewModel>
        {
            new() { LanguageCode = "nl", Name = "Naam", Topic = "", Introduction = "" },
        };

        var result = ProjectTranslationForm.PrepareSubmission(
            translations, new[] { "nl" }, requireTopicAndIntro: true);

        Assert.Contains(result.Errors, e => e.FieldKey == "Translations[0].Topic" && e.ResourceKey == "Project_TopicRequired");
        Assert.Contains(result.Errors, e => e.FieldKey == "Translations[0].Introduction" && e.ResourceKey == "Project_IntroductionRequired");
    }

    [Fact]
    public void PrepareSubmission_NotRequireFull_BlankTopicAndIntro_NoError()
    {
        var translations = new List<ProjectTranslationViewModel>
        {
            new() { LanguageCode = "nl", Name = "Naam", Topic = "", Introduction = "" },
        };

        var result = ProjectTranslationForm.PrepareSubmission(
            translations, new[] { "nl" }, requireTopicAndIntro: false);

        Assert.Empty(result.Errors);
    }

    [Fact]
    public void PrepareSubmission_FieldKeyUsesOriginalIndex_NotFilteredPosition()
    {
        // en at index 0, nl at index 1; only nl supported and its Name blank.
        // The error must point at index 1, the position in the SUBMITTED list,
        // so it binds back to the right tab on re-render.
        var translations = new List<ProjectTranslationViewModel>
        {
            new() { LanguageCode = "en", Name = "Name", Topic = "T", Introduction = "I" },
            new() { LanguageCode = "nl", Name = "", Topic = "T", Introduction = "I" },
        };

        var result = ProjectTranslationForm.PrepareSubmission(
            translations, new[] { "nl" }, requireTopicAndIntro: false);

        var error = Assert.Single(result.Errors);
        Assert.Equal("Translations[1].Name", error.FieldKey);
    }

    [Fact]
    public void PrepareSubmission_SupportedCodes_DedupedAndTrimmedOfBlanks()
    {
        var result = ProjectTranslationForm.PrepareSubmission(
            Bilingual(), new[] { "nl", "nl", "", "en" }, requireTopicAndIntro: false);

        Assert.Equal(new[] { "nl", "en" }, result.SupportedCodes);
    }

    [Fact]
    public void PrepareSubmission_NullInputs_ReturnEmptyResult()
    {
        var result = ProjectTranslationForm.PrepareSubmission(null, null, requireTopicAndIntro: true);

        Assert.Empty(result.SupportedCodes);
        Assert.Empty(result.ActiveTranslations);
        Assert.Empty(result.Errors);
    }

    #endregion
}
