using SynapseEnqueteManagement.BL.Domain.I18n.Entities;
using SynapseEnqueteManagement.BL.Helpers;

namespace SynapseEnqueteManagement.BL.Tests.Unit;

public class CategoryNameResolverTests
{
    private static readonly Language LanguageNl = new() { Code = "nl", DisplayName = "Nederlands", IsActive = true };
    private static readonly Language LanguageEn = new() { Code = "en", DisplayName = "English", IsActive = true };

    [Fact]
    public void Resolve_LanguageMatch_ReturnsMatchingTranslation()
    {
        var category = new Category
        {
            Translations = new List<CategoryTranslation>
            {
                new() { Language = LanguageNl, Name = "Welzijn" },
                new() { Language = LanguageEn, Name = "Wellbeing" },
            }
        };

        Assert.Equal("Welzijn", CategoryNameResolver.Resolve(category, "nl"));
        Assert.Equal("Wellbeing", CategoryNameResolver.Resolve(category, "en"));
    }

    [Fact]
    public void Resolve_LanguageNotInTranslations_FallsBackToFirstAvailable()
    {
        var category = new Category
        {
            Translations = new List<CategoryTranslation>
            {
                new() { Language = LanguageNl, Name = "Welzijn" }
            }
        };

        Assert.Equal("Welzijn", CategoryNameResolver.Resolve(category, "fr"));
    }

    [Fact]
    public void Resolve_NullCategory_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, CategoryNameResolver.Resolve(null, "nl"));
    }

    [Fact]
    public void Resolve_NullTranslations_ReturnsEmpty()
    {
        var category = new Category { Translations = null };
        Assert.Equal(string.Empty, CategoryNameResolver.Resolve(category, "nl"));
    }

    [Fact]
    public void Resolve_EmptyTranslations_ReturnsEmpty()
    {
        var category = new Category { Translations = new List<CategoryTranslation>() };
        Assert.Equal(string.Empty, CategoryNameResolver.Resolve(category, "nl"));
    }
}
