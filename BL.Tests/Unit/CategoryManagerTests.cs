using SynapseEnqueteManagement.BL.Domain.I18n.Entities;
using SynapseEnqueteManagement.DAL.Interfaces;

namespace SynapseEnqueteManagement.BL.Tests.Unit;

public class CategoryManagerTests
{
    private readonly Mock<ICategoryRepository> _categoryRepo = new(MockBehavior.Strict);
    private readonly Mock<ILanguageRepository> _languageRepo = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _uow = new(MockBehavior.Strict);
    private readonly CategoryManager _manager;

    public CategoryManagerTests()
    {
        _manager = new CategoryManager(_categoryRepo.Object, _languageRepo.Object, _uow.Object);
    }

    [Fact]
    public async Task SetIdeaModerationStatusAsync_IdeaExists_UpdatesStatusAndSaves()
    {
        var idea = new Idea { IdeaId = 1, Status = ModerationStatus.Unflagged };
        _categoryRepo.Setup(r => r.ReadIdeaByIdForUpdateAsync(1, It.IsAny<int?>()))
            .ReturnsAsync(idea)
            .Verifiable(Times.Once());
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .Verifiable(Times.Once());

        await _manager.SetIdeaModerationStatusAsync(1, ModerationStatus.Approved);

        Assert.Equal(ModerationStatus.Approved, idea.Status);
        _categoryRepo.VerifyAll();
        _uow.VerifyAll();
    }

    [Fact]
    public async Task SetIdeaModerationStatusAsync_IdeaNotFound_Throws()
    {
        _categoryRepo.Setup(r => r.ReadIdeaByIdForUpdateAsync(99, It.IsAny<int?>()))
            .ReturnsAsync((Idea)null)
            .Verifiable(Times.Once());

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _manager.SetIdeaModerationStatusAsync(99, ModerationStatus.Approved));

        _categoryRepo.VerifyAll();
    }

    [Fact]
    public async Task CreateCategoryAsync_AllSupportedLanguagesProvided_PersistsTranslations()
    {
        var langNl = new Language { Code = "nl", DisplayName = "Nederlands", IsActive = true };
        var langEn = new Language { Code = "en", DisplayName = "English", IsActive = true };
        var project = new Project
        {
            ProjectId = 1,
            SupportedLanguages = new List<Language> { langNl, langEn },
        };

        var translations = new[]
        {
            new CategoryTranslationInput("nl", "Campus welzijn", "Welzijn op school"),
            new CategoryTranslationInput("en", "Campus wellbeing", "Wellbeing at school"),
        };

        _languageRepo.Setup(r => r.ReadByCodesForUpdateAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new List<Language> { langNl, langEn })
            .Verifiable(Times.Once());

        Category saved = null;
        _categoryRepo.Setup(r => r.CreateAsync(It.IsAny<Category>()))
            .Callback<Category>(c => saved = c)
            .ReturnsAsync((Category c) => c)
            .Verifiable(Times.Once());
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .Verifiable(Times.Once());

        await _manager.CreateCategoryAsync(project, translations);

        Assert.NotNull(saved);
        Assert.Equal(2, saved.Translations.Count);
        Assert.Contains(saved.Translations, t => t.Language.Code == "nl" && t.Name == "Campus welzijn");
        Assert.Contains(saved.Translations, t => t.Language.Code == "en" && t.Name == "Campus wellbeing");
        _categoryRepo.VerifyAll();
        _languageRepo.VerifyAll();
        _uow.VerifyAll();
    }

    [Fact]
    public async Task CreateCategoryAsync_MissingLanguageCode_ThrowsValidationException()
    {
        _languageRepo.Setup(r => r.ReadByCodesForUpdateAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new List<Language>())
            .Verifiable(Times.Once());

        var project = new Project { ProjectId = 1 };
        var translations = new[] { new CategoryTranslationInput("xx", "Test", null) };

        await Assert.ThrowsAnyAsync<System.ComponentModel.DataAnnotations.ValidationException>(() =>
            _manager.CreateCategoryAsync(project, translations));

        _languageRepo.VerifyAll();
    }

    [Fact]
    public async Task CreateCategoryAsync_DuplicateLanguageCodes_ThrowsValidationException()
    {
        var project = new Project { ProjectId = 1 };
        var translations = new[]
        {
            new CategoryTranslationInput("nl", "A", null),
            new CategoryTranslationInput("nl", "B", null),
        };

        await Assert.ThrowsAnyAsync<System.ComponentModel.DataAnnotations.ValidationException>(() =>
            _manager.CreateCategoryAsync(project, translations));
    }
}
