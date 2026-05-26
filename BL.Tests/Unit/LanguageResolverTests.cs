using SynapseEnqueteManagement.BL.Domain.I18n.Entities;
using SynapseEnqueteManagement.BL.Exceptions;

namespace SynapseEnqueteManagement.BL.Tests.Unit;

public class LanguageResolverTests
{
    private readonly Mock<ILanguageRepository> _languageRepo = new(MockBehavior.Strict);
    private readonly LanguageResolver _resolver;

    private static readonly Language Nl = new() { Code = "nl", DisplayName = "Nederlands", IsActive = true };
    private static readonly Language En = new() { Code = "en", DisplayName = "English", IsActive = true };
    private static readonly Language InactiveFr = new() { Code = "fr", DisplayName = "Français", IsActive = false };

    public LanguageResolverTests()
    {
        _resolver = new LanguageResolver(_languageRepo.Object);
    }

    [Fact]
    public async Task ResolveAsync_AllCodesKnownAndActive_ReturnsDictionaryByCode()
    {
        _languageRepo.Setup(r => r.ReadByCodesForUpdateAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new[] { Nl, En });

        var result = await _resolver.ResolveAsync(new[] { "nl", "en" });

        Assert.Equal(2, result.Count);
        Assert.Same(Nl, result["nl"]);
        Assert.Same(En, result["en"]);
    }

    [Fact]
    public async Task ResolveAsync_UnknownCode_ThrowsLocalizedValidation()
    {
        _languageRepo.Setup(r => r.ReadByCodesForUpdateAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new[] { Nl });

        var ex = await Assert.ThrowsAsync<LocalizedValidationException>(() =>
            _resolver.ResolveAsync(new[] { "nl", "zz" }));

        Assert.Equal("Validation.Language.UnknownCodes", ex.Code);
    }

    [Fact]
    public async Task ResolveAsync_InactiveAndRequireActive_ThrowsLocalizedValidation()
    {
        _languageRepo.Setup(r => r.ReadByCodesForUpdateAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new[] { Nl, InactiveFr });

        var ex = await Assert.ThrowsAsync<LocalizedValidationException>(() =>
            _resolver.ResolveAsync(new[] { "nl", "fr" }));

        Assert.Equal("Validation.Project.InactiveLanguageNotAllowed", ex.Code);
    }

    [Fact]
    public async Task ResolveAsync_InactiveButRequireActiveFalse_ReturnsInactive()
    {
        _languageRepo.Setup(r => r.ReadByCodesForUpdateAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new[] { InactiveFr });

        var result = await _resolver.ResolveAsync(new[] { "fr" }, requireActive: false);

        Assert.Single(result);
        Assert.Same(InactiveFr, result["fr"]);
    }

    [Fact]
    public async Task ResolveAsync_EmptyInput_ReturnsEmptyDictionary()
    {
        var result = await _resolver.ResolveAsync(Array.Empty<string>());

        Assert.Empty(result);
        _languageRepo.Verify(r => r.ReadByCodesForUpdateAsync(It.IsAny<IEnumerable<string>>()), Times.Never);
    }

    [Fact]
    public async Task ResolveAsync_DuplicateCodes_DeduplicatesBeforeQuerying()
    {
        IEnumerable<string> captured = null;
        _languageRepo.Setup(r => r.ReadByCodesForUpdateAsync(It.IsAny<IEnumerable<string>>()))
            .Callback<IEnumerable<string>>(c => captured = c.ToList())
            .ReturnsAsync(new[] { Nl });

        var result = await _resolver.ResolveAsync(new[] { "nl", "nl", "nl" });

        Assert.Single(result);
        Assert.Single(captured);
    }

    [Fact]
    public async Task ResolveSingleAsync_Known_ReturnsLanguage()
    {
        _languageRepo.Setup(r => r.ReadByCodeForUpdateAsync("nl")).ReturnsAsync(Nl);

        var result = await _resolver.ResolveSingleAsync("nl");

        Assert.Same(Nl, result);
    }

    [Fact]
    public async Task ResolveSingleAsync_Unknown_ThrowsKeyNotFound()
    {
        _languageRepo.Setup(r => r.ReadByCodeForUpdateAsync("xx")).ReturnsAsync((Language)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _resolver.ResolveSingleAsync("xx"));
    }

    [Fact]
    public async Task ResolveSingleAsync_InactiveAndRequireActive_ThrowsKeyNotFound()
    {
        _languageRepo.Setup(r => r.ReadByCodeForUpdateAsync("fr")).ReturnsAsync(InactiveFr);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _resolver.ResolveSingleAsync("fr"));
    }

    [Fact]
    public async Task ResolveSingleAsync_InactiveButRequireActiveFalse_ReturnsInactive()
    {
        _languageRepo.Setup(r => r.ReadByCodeForUpdateAsync("fr")).ReturnsAsync(InactiveFr);

        var result = await _resolver.ResolveSingleAsync("fr", requireActive: false);

        Assert.Same(InactiveFr, result);
    }

    [Fact]
    public async Task ResolveForTranslationsAsync_Empty_ThrowsLocalizedValidation()
    {
        var ex = await Assert.ThrowsAsync<LocalizedValidationException>(() =>
            _resolver.ResolveForTranslationsAsync(Array.Empty<string>()));

        Assert.Equal("Validation.Translation.AtLeastOne", ex.Code);
    }

    [Fact]
    public async Task ResolveForTranslationsAsync_DuplicateCodes_ThrowsLocalizedValidation()
    {
        var ex = await Assert.ThrowsAsync<LocalizedValidationException>(() =>
            _resolver.ResolveForTranslationsAsync(new[] { "nl", "nl" }));

        Assert.Equal("Validation.Translation.OneRowPerLanguage", ex.Code);
    }

    [Fact]
    public async Task ResolveForTranslationsAsync_DistinctCodes_DelegatesToResolveAsync()
    {
        _languageRepo.Setup(r => r.ReadByCodesForUpdateAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new[] { Nl, En });

        var result = await _resolver.ResolveForTranslationsAsync(new[] { "nl", "en" });

        Assert.Equal(2, result.Count);
    }
}
