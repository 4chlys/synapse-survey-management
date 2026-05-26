using SynapseEnqueteManagement.BL.Helpers;

namespace SynapseEnqueteManagement.BL.Tests.Unit;

public class TranslationPickerTests
{
    private static readonly Language LanguageNl = new() { Code = "nl", DisplayName = "Nederlands", IsActive = true };
    private static readonly Language LanguageEn = new() { Code = "en", DisplayName = "English", IsActive = true };

    private static CategoryTranslation Nl() => new() { Language = LanguageNl, Name = "Welzijn" };
    private static CategoryTranslation En() => new() { Language = LanguageEn, Name = "Wellbeing" };

    [Fact]
    public void Pick_LanguageMatch_ReturnsMatchingTranslation()
    {
        var nl = Nl();
        var en = En();
        var translations = new[] { nl, en };

        Assert.Same(nl, TranslationPicker.Pick(translations, "nl"));
        Assert.Same(en, TranslationPicker.Pick(translations, "en"));
    }

    [Fact]
    public void Pick_LanguageAbsent_FallsBackToFirst()
    {
        var nl = Nl();
        var en = En();
        var translations = new[] { nl, en };

        Assert.Same(nl, TranslationPicker.Pick(translations, "fr"));
    }

    [Fact]
    public void Pick_NullLanguageCodeRequested_FallsBackToFirst()
    {
        var nl = Nl();
        var translations = new[] { nl, En() };

        Assert.Same(nl, TranslationPicker.Pick(translations, null));
    }

    [Fact]
    public void Pick_TranslationWithNullLanguage_DoesNotThrowAndFallsBackToFirst()
    {
        var orphan = new CategoryTranslation { Language = null, Name = "Orphan" };
        var en = En();
        var translations = new[] { orphan, en };

        // No translation matches "nl" (orphan has null Language), so it falls back to the first entry.
        Assert.Same(orphan, TranslationPicker.Pick(translations, "nl"));
    }

    [Fact]
    public void Pick_NullCollection_ReturnsNull()
    {
        Assert.Null(TranslationPicker.Pick<CategoryTranslation>(null, "nl"));
    }

    [Fact]
    public void Pick_EmptyCollection_ReturnsNull()
    {
        Assert.Null(TranslationPicker.Pick(Array.Empty<CategoryTranslation>(), "nl"));
    }
}
