using SynapseEnqueteManagement.BL.Domain.I18n.Entities;
using SynapseEnqueteManagement.BL.Exceptions;

namespace SynapseEnqueteManagement.BL.Tests.Unit;

public class ProjectTranslationValidatorTests
{
    private readonly Mock<ILanguageResolver> _resolver = new(MockBehavior.Strict);
    private readonly ProjectTranslationValidator _validator;

    private static readonly Language Nl = new() { Code = "nl", DisplayName = "Nederlands", IsActive = true };
    private static readonly Language En = new() { Code = "en", DisplayName = "English", IsActive = true };

    public ProjectTranslationValidatorTests()
    {
        _validator = new ProjectTranslationValidator(_resolver.Object);

        _resolver.Setup(r => r.ResolveAsync(It.IsAny<IEnumerable<string>>(), true))
            .ReturnsAsync((IEnumerable<string> codes, bool _) =>
            {
                var set = codes.ToHashSet();
                return (IReadOnlyDictionary<string, Language>)new[] { Nl, En }
                    .Where(l => set.Contains(l.Code))
                    .ToDictionary(l => l.Code);
            });
    }

    private static ProjectTranslationInput[] Translations(params string[] codes) =>
        codes.Select(c => new ProjectTranslationInput(c, c.ToUpperInvariant(), "topic")).ToArray();

    private static IntroductionTranslationInput[] Intros(params string[] codes) =>
        codes.Select(c => new IntroductionTranslationInput(c, $"text-{c}")).ToArray();

    [Fact]
    public async Task ValidateAsync_HappyPath_ReturnsLanguagesDictionary()
    {
        var result = await _validator.ValidateAsync(
            Translations("nl", "en"),
            new[] { "nl", "en" },
            "nl",
            Intros("nl"));

        Assert.Equal(2, result.Count);
        Assert.True(result.ContainsKey("nl"));
        Assert.True(result.ContainsKey("en"));
    }

    [Fact]
    public async Task ValidateAsync_BlankDefaultLanguageCode_Throws()
    {
        var ex = await Assert.ThrowsAsync<LocalizedValidationException>(() =>
            _validator.ValidateAsync(
                Translations("nl"),
                new[] { "nl" },
                "",
                Array.Empty<IntroductionTranslationInput>()));
        Assert.Equal("Validation.Project.DefaultLanguageCodeRequired", ex.Code);
    }

    [Fact]
    public async Task ValidateAsync_NoSupportedLanguages_Throws()
    {
        var ex = await Assert.ThrowsAsync<LocalizedValidationException>(() =>
            _validator.ValidateAsync(
                Translations("nl"),
                Array.Empty<string>(),
                "nl",
                Array.Empty<IntroductionTranslationInput>()));
        Assert.Equal("Validation.Project.AtLeastOneSupportedLanguage", ex.Code);
    }

    [Fact]
    public async Task ValidateAsync_DefaultNotInSupported_Throws()
    {
        var ex = await Assert.ThrowsAsync<LocalizedValidationException>(() =>
            _validator.ValidateAsync(
                Translations("nl"),
                new[] { "nl" },
                "en",
                Array.Empty<IntroductionTranslationInput>()));
        Assert.Equal("Validation.Project.DefaultLanguageMustBeSupported", ex.Code);
    }

    [Fact]
    public async Task ValidateAsync_DuplicateTranslationCodes_Throws()
    {
        var inputs = new[]
        {
            new ProjectTranslationInput("nl", "A", "T"),
            new ProjectTranslationInput("nl", "B", "T"),
        };
        var ex = await Assert.ThrowsAsync<LocalizedValidationException>(() =>
            _validator.ValidateAsync(
                inputs,
                new[] { "nl" },
                "nl",
                Array.Empty<IntroductionTranslationInput>()));
        Assert.Equal("Validation.Translation.OneRowPerLanguage", ex.Code);
    }

    [Fact]
    public async Task ValidateAsync_MissingTranslationForSupportedLanguage_Throws()
    {
        var ex = await Assert.ThrowsAsync<LocalizedValidationException>(() =>
            _validator.ValidateAsync(
                Translations("nl"),
                new[] { "nl", "en" },
                "nl",
                Array.Empty<IntroductionTranslationInput>()));
        Assert.Equal("Validation.Project.AllSupportedLanguagesNeedTranslation", ex.Code);
    }

    [Fact]
    public async Task ValidateAsync_DuplicateIntroductionCodes_Throws()
    {
        var ex = await Assert.ThrowsAsync<LocalizedValidationException>(() =>
            _validator.ValidateAsync(
                Translations("nl"),
                new[] { "nl" },
                "nl",
                new[]
                {
                    new IntroductionTranslationInput("nl", "a"),
                    new IntroductionTranslationInput("nl", "b"),
                }));
        Assert.Equal("Validation.Project.IntroductionsOneRowPerLanguage", ex.Code);
    }

    [Fact]
    public async Task ValidateAsync_DelegatesUnknownAndInactiveDecisionsToResolver()
    {
        // If the resolver throws, the validator must surface that as-is.
        _resolver.Reset();
        _resolver.Setup(r => r.ResolveAsync(It.IsAny<IEnumerable<string>>(), true))
            .ThrowsAsync(new LocalizedValidationException("Validation.Language.UnknownCodes", "zz"));

        var ex = await Assert.ThrowsAsync<LocalizedValidationException>(() =>
            _validator.ValidateAsync(
                Translations("zz"),
                new[] { "zz" },
                "zz",
                Array.Empty<IntroductionTranslationInput>()));
        Assert.Equal("Validation.Language.UnknownCodes", ex.Code);
    }
}
