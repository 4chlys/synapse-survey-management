using System.ComponentModel.DataAnnotations;
using SynapseEnqueteManagement.BL.Domain.I18n.Entities;
using SynapseEnqueteManagement.BL.Exceptions;
using SynapseEnqueteManagement.BL.Implementation;

namespace SynapseEnqueteManagement.BL.Tests.Unit;

public class ProjectManagerTests
{
    private readonly Mock<IProjectRepository> _projectRepository = new(MockBehavior.Strict);
    private readonly Mock<IEmojiManager> _emojiManager = new(MockBehavior.Strict);
    private readonly Mock<IProjectTranslationValidator> _translationValidator = new(MockBehavior.Strict);
    private readonly Mock<IShortFormService> _shortFormService = new(MockBehavior.Strict);
    private readonly Mock<IProjectDuplicationService> _duplicationService = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _uow = new(MockBehavior.Strict);
    private readonly ProjectManager _manager;

    private const int ProjectId = 1;
    private const int EmojiId = 99;

    private static readonly Language LanguageNl = new() { Code = "nl", DisplayName = "Nederlands", IsActive = true };
    private static readonly Language LanguageEn = new() { Code = "en", DisplayName = "English", IsActive = true };

    public ProjectManagerTests()
    {
        _manager = new ProjectManager(
            _projectRepository.Object,
            _emojiManager.Object,
            _translationValidator.Object,
            _shortFormService.Object,
            _duplicationService.Object,
            _uow.Object);

        _translationValidator
            .Setup(v => v.ValidateAsync(
                It.IsAny<ICollection<ProjectTranslationInput>>(),
                It.IsAny<ICollection<string>>(),
                It.IsAny<string>(),
                It.IsAny<ICollection<IntroductionTranslationInput>>()))
            .ReturnsAsync((
                ICollection<ProjectTranslationInput> translations,
                ICollection<string> supported,
                string defaultCode,
                ICollection<IntroductionTranslationInput> _) =>
            {
                if (string.IsNullOrWhiteSpace(defaultCode))
                    throw new LocalizedValidationException("Validation.Project.DefaultLanguageCodeRequired");
                if (supported.Count == 0)
                    throw new LocalizedValidationException("Validation.Project.AtLeastOneSupportedLanguage");
                if (!supported.Contains(defaultCode))
                    throw new LocalizedValidationException("Validation.Project.DefaultLanguageMustBeSupported");
                var codes = translations.Select(t => t.LanguageCode).ToList();
                if (!supported.All(codes.Contains))
                    throw new LocalizedValidationException("Validation.Project.AllSupportedLanguagesNeedTranslation");
                var pool = new[] { LanguageNl, LanguageEn };
                var byCode = pool.Where(l => codes.Contains(l.Code)).ToDictionary(l => l.Code);
                var missing = codes.Where(c => !byCode.ContainsKey(c)).ToList();
                if (missing.Count > 0)
                    throw new LocalizedValidationException("Validation.Language.UnknownCodes", string.Join(", ", missing));
                return (IReadOnlyDictionary<string, Language>)byCode;
            });
    }

    private static IEnumerable<ProjectTranslationInput> Translations(params (string code, string name)[] entries) =>
        entries.Select(e => new ProjectTranslationInput(e.code, e.name, "T"));

    private static IEnumerable<IntroductionTranslationInput> NoIntroductions() =>
        Array.Empty<IntroductionTranslationInput>();

    #region CreateProjectAsync

    [Fact]
    public async Task CreateProjectAsync_BlankShortForm_AutofillsFromDefaultLanguageName()
    {
        // Arrange
        _shortFormService.Setup(s => s.Normalize("My Project")).Returns("my-project");
        _shortFormService.Setup(s => s.IsValid("my-project")).Returns(true);
        _shortFormService.Setup(s => s.IsReserved("my-project")).Returns(false);
        _shortFormService.Setup(s => s.EnsureUniqueAsync("my-project", null)).ReturnsAsync("my-project");
        _projectRepository.Setup(r => r.CreateAsync(It.IsAny<Project>())).ReturnsAsync((Project p) => p);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _manager.CreateProjectAsync(
            Translations(("nl", "Mijn Project"), ("en", "My Project")),
            NoIntroductions(),
            defaultLanguageCode: "en",
            supportedLanguageCodes: new[] { "nl", "en" },
            string.Empty, FormType.VerticalScroll,
            null, null, false, platformId: 1, shortForm: null,
            allowCrossLanguageIdeas: true);

        // Assert
        Assert.Equal("my-project", result.ShortForm);
        Assert.Equal("en", result.DefaultLanguage.Code);
        Assert.Equal(2, result.Translations.Count);
        Assert.True(result.AllowCrossLanguageIdeas);
    }

    [Fact]
    public async Task CreateProjectAsync_AllowCrossLanguageIdeasFalse_DefaultsToFalse()
    {
        // Arrange
        _shortFormService.Setup(s => s.Normalize("Naam")).Returns("naam");
        _shortFormService.Setup(s => s.IsValid("naam")).Returns(true);
        _shortFormService.Setup(s => s.IsReserved("naam")).Returns(false);
        _shortFormService.Setup(s => s.EnsureUniqueAsync("naam", null)).ReturnsAsync("naam");
        _projectRepository.Setup(r => r.CreateAsync(It.IsAny<Project>())).ReturnsAsync((Project p) => p);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act — omit allowCrossLanguageIdeas to verify default is false
        var result = await _manager.CreateProjectAsync(
            Translations(("nl", "Naam")),
            NoIntroductions(),
            "nl", new[] { "nl" },
            string.Empty, FormType.VerticalScroll,
            null, null, false, platformId: 1);

        // Assert
        Assert.False(result.AllowCrossLanguageIdeas);
    }

    [Fact]
    public async Task CreateProjectAsync_ReservedShortForm_ThrowsValidationException()
    {
        // Arrange
        _shortFormService.Setup(s => s.IsValid("admin")).Returns(true);
        _shortFormService.Setup(s => s.IsReserved("admin")).Returns(true);

        // Act & Assert
        await Assert.ThrowsAnyAsync<ValidationException>(() =>
            _manager.CreateProjectAsync(
                Translations(("nl", "Admin")),
                NoIntroductions(),
                "nl", new[] { "nl" },
                string.Empty, FormType.VerticalScroll,
                null, null, false, platformId: 1, shortForm: "admin"));
    }

    [Fact]
    public async Task CreateProjectAsync_InvalidShortForm_ThrowsValidationException()
    {
        // Arrange
        _shortFormService.Setup(s => s.IsValid("INVALID FORM")).Returns(false);

        // Act & Assert
        await Assert.ThrowsAnyAsync<ValidationException>(() =>
            _manager.CreateProjectAsync(
                Translations(("nl", "Test")),
                NoIntroductions(),
                "nl", new[] { "nl" },
                string.Empty, FormType.VerticalScroll,
                null, null, false, platformId: 1, shortForm: "INVALID FORM"));
    }

    [Fact]
    public async Task CreateProjectAsync_DefaultLanguageNotInSupported_Throws()
    {
        // Act & Assert
        await Assert.ThrowsAnyAsync<ValidationException>(() =>
            _manager.CreateProjectAsync(
                Translations(("nl", "X")),
                NoIntroductions(),
                defaultLanguageCode: "en",
                supportedLanguageCodes: new[] { "nl" },
                string.Empty, FormType.VerticalScroll,
                null, null, false, platformId: 1));
    }

    [Fact]
    public async Task CreateProjectAsync_MissingTranslationForSupportedLanguage_Throws()
    {
        // Act & Assert
        await Assert.ThrowsAnyAsync<ValidationException>(() =>
            _manager.CreateProjectAsync(
                Translations(("nl", "X")),
                NoIntroductions(),
                "nl", new[] { "nl", "en" },
                string.Empty, FormType.VerticalScroll,
                null, null, false, platformId: 1));
    }

    [Fact]
    public async Task CreateProjectAsync_UnknownLanguageCode_Throws()
    {
        // Act & Assert
        await Assert.ThrowsAnyAsync<ValidationException>(() =>
            _manager.CreateProjectAsync(
                Translations(("zz", "X")),
                NoIntroductions(),
                "zz", new[] { "zz" },
                string.Empty, FormType.VerticalScroll,
                null, null, false, platformId: 1));
    }

    [Theory]
    [InlineData("#FFEB3B")] // bright yellow — fails contrast against the #FFFFFF light base
    [InlineData("#000000")] // black — fails contrast against the #1A1430 dark base
    public async Task CreateProjectAsync_LowContrastPrimaryColor_ThrowsValidationException(string primaryColor)
    {
        // Arrange
        _shortFormService.Setup(s => s.Normalize("Test")).Returns("test");
        _shortFormService.Setup(s => s.IsValid("test")).Returns(true);
        _shortFormService.Setup(s => s.IsReserved("test")).Returns(false);
        _shortFormService.Setup(s => s.EnsureUniqueAsync("test", null)).ReturnsAsync("test");

        // Act & Assert
        await Assert.ThrowsAnyAsync<ValidationException>(() =>
            _manager.CreateProjectAsync(
                Translations(("nl", "Test")),
                NoIntroductions(),
                "nl", new[] { "nl" },
                string.Empty, FormType.VerticalScroll,
                null, null, false, platformId: 1,
                primaryColor: primaryColor));
    }

    [Fact]
    public async Task CreateProjectAsync_WellContrastedPrimaryColor_Succeeds()
    {
        // Arrange — #5C6BC0 (indigo 400) clears 3:1 against both #FFFFFF and the dark base.
        _shortFormService.Setup(s => s.Normalize("Test")).Returns("test");
        _shortFormService.Setup(s => s.IsValid("test")).Returns(true);
        _shortFormService.Setup(s => s.IsReserved("test")).Returns(false);
        _shortFormService.Setup(s => s.EnsureUniqueAsync("test", null)).ReturnsAsync("test");
        _projectRepository.Setup(r => r.CreateAsync(It.IsAny<Project>())).ReturnsAsync((Project p) => p);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _manager.CreateProjectAsync(
            Translations(("nl", "Test")),
            NoIntroductions(),
            "nl", new[] { "nl" },
            string.Empty, FormType.VerticalScroll,
            null, null, false, platformId: 1,
            primaryColor: "#5C6BC0");

        // Assert
        Assert.Equal("#5C6BC0", result.PrimaryColor);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task CreateProjectAsync_BlankPrimaryColor_Succeeds(string primaryColor)
    {
        // Arrange — empty / null means "use the theme default", so the contrast guard is skipped.
        _shortFormService.Setup(s => s.Normalize("Test")).Returns("test");
        _shortFormService.Setup(s => s.IsValid("test")).Returns(true);
        _shortFormService.Setup(s => s.IsReserved("test")).Returns(false);
        _shortFormService.Setup(s => s.EnsureUniqueAsync("test", null)).ReturnsAsync("test");
        _projectRepository.Setup(r => r.CreateAsync(It.IsAny<Project>())).ReturnsAsync((Project p) => p);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _manager.CreateProjectAsync(
            Translations(("nl", "Test")),
            NoIntroductions(),
            "nl", new[] { "nl" },
            string.Empty, FormType.VerticalScroll,
            null, null, false, platformId: 1,
            primaryColor: primaryColor);

        // Assert
        Assert.Equal(primaryColor, result.PrimaryColor);
    }

    #endregion

    #region UpdateProjectAsync

    private static Project SeedExistingProject(ProjectState state, string shortForm = "old-form")
    {
        var nlTranslation = new ProjectTranslation { Language = LanguageNl, Name = "Naam", Topic = "T" };
        var project = new Project
        {
            ProjectId = ProjectId,
            ShortForm = shortForm,
            ProjectState = state,
            DefaultLanguage = LanguageNl,
            SupportedLanguages = new List<Language> { LanguageNl },
            Translations = new List<ProjectTranslation> { nlTranslation },
        };
        nlTranslation.Project = project;
        return project;
    }

    [Fact]
    public async Task UpdateProjectAsync_ShortFormChangeInDraft_UpdatesShortForm()
    {
        // Arrange
        var project = SeedExistingProject(ProjectState.Draft);
        _projectRepository.Setup(r => r.ReadByIdWithTranslationsAndLanguagesForUpdateAsync(ProjectId))
            .ReturnsAsync(project);
        _shortFormService.Setup(s => s.IsValid("new-form")).Returns(true);
        _shortFormService.Setup(s => s.IsReserved("new-form")).Returns(false);
        _shortFormService.Setup(s => s.EnsureUniqueAsync("new-form", ProjectId)).ReturnsAsync("new-form");
        _projectRepository.Setup(r => r.Update(project));
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _manager.UpdateProjectAsync(
            ProjectId,
            Translations(("nl", "Naam")),
            NoIntroductions(),
            "nl", new[] { "nl" },
            string.Empty, FormType.VerticalScroll,
            null, null, false, shortForm: "new-form",
            allowCrossLanguageIdeas: true);

        // Assert
        Assert.Equal("new-form", result.ShortForm);
        Assert.True(result.AllowCrossLanguageIdeas);
    }

    [Fact]
    public async Task UpdateProjectAsync_ShortFormChangeInActive_ThrowsValidationException()
    {
        // Arrange
        var project = SeedExistingProject(ProjectState.Active);
        _projectRepository.Setup(r => r.ReadByIdWithTranslationsAndLanguagesForUpdateAsync(ProjectId))
            .ReturnsAsync(project);

        // Act & Assert
        await Assert.ThrowsAnyAsync<ValidationException>(() =>
            _manager.UpdateProjectAsync(
                ProjectId,
                Translations(("nl", "Naam")),
                NoIntroductions(),
                "nl", new[] { "nl" },
                string.Empty, FormType.VerticalScroll,
                null, null, false, shortForm: "changed-form"));
    }

    [Fact]
    public async Task UpdateProjectAsync_SameShortFormInActive_DoesNotThrow()
    {
        // Arrange
        var project = SeedExistingProject(ProjectState.Active, shortForm: "same-form");
        _projectRepository.Setup(r => r.ReadByIdWithTranslationsAndLanguagesForUpdateAsync(ProjectId))
            .ReturnsAsync(project);
        _projectRepository.Setup(r => r.Update(project));
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act — passing the same shortform should not throw
        var result = await _manager.UpdateProjectAsync(
            ProjectId,
            Translations(("nl", "Naam")),
            NoIntroductions(),
            "nl", new[] { "nl" },
            string.Empty, FormType.VerticalScroll,
            null, null, false, shortForm: "same-form");

        // Assert
        Assert.Equal("same-form", result.ShortForm);
    }

    #endregion

    #region EnableEmojiForProjectAsync

    [Fact]
    public async Task EnableEmojiForProjectAsync_HappyPath_AttachesEmojiAndSavesOnce()
    {
        // Arrange
        var project = new Project { ProjectId = ProjectId, Emojis = new List<Emoji>() };
        var emoji = new Emoji { EmojiId = EmojiId, Symbol = "👍", Name = "thumbsup" };
        _projectRepository.Setup(r => r.ReadByIdWithEmojisForUpdateAsync(ProjectId))
            .ReturnsAsync(project);
        _emojiManager.Setup(r => r.GetByIdForUpdateAsync(EmojiId))
            .ReturnsAsync(emoji);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .Verifiable(Times.Exactly(1));

        // Act
        var result = await _manager.EnableEmojiForProjectAsync(ProjectId, EmojiId);

        // Assert
        Assert.True(result);
        Assert.Contains(emoji, project.Emojis);
        _projectRepository.VerifyAll();
        _uow.VerifyAll();
    }

    [Fact]
    public async Task EnableEmojiForProjectAsync_ForwardsCancellationTokenToUnitOfWork()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;
        var project = new Project { ProjectId = ProjectId, Emojis = new List<Emoji>() };
        _projectRepository.Setup(r => r.ReadByIdWithEmojisForUpdateAsync(ProjectId))
            .ReturnsAsync(project);
        _emojiManager.Setup(r => r.GetByIdForUpdateAsync(EmojiId))
            .ReturnsAsync(new Emoji { EmojiId = EmojiId, Symbol = "👍", Name = "thumbsup" });
        _projectRepository.Setup(r => r.Update(project));
        _uow.Setup(u => u.SaveChangesAsync(ct))
            .ReturnsAsync(1)
            .Verifiable(Times.Exactly(1));

        // Act
        await _manager.EnableEmojiForProjectAsync(ProjectId, EmojiId, ct);

        // Assert
        _uow.VerifyAll();
    }

    [Fact]
    public async Task EnableEmojiForProjectAsync_ProjectNotFound_ReturnsFalseWithoutSaving()
    {
        // Arrange
        _projectRepository.Setup(r => r.ReadByIdWithEmojisForUpdateAsync(ProjectId))
            .ReturnsAsync((Project)null);

        // Act
        var result = await _manager.EnableEmojiForProjectAsync(ProjectId, EmojiId);

        // Assert
        Assert.False(result);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EnableEmojiForProjectAsync_EmojiNotFound_ReturnsFalseWithoutSaving()
    {
        // Arrange
        _projectRepository.Setup(r => r.ReadByIdWithEmojisForUpdateAsync(ProjectId))
            .ReturnsAsync(new Project { ProjectId = ProjectId, Emojis = new List<Emoji>() });
        _emojiManager.Setup(r => r.GetByIdForUpdateAsync(EmojiId))
            .ReturnsAsync((Emoji)null);

        // Act
        var result = await _manager.EnableEmojiForProjectAsync(ProjectId, EmojiId);

        // Assert
        Assert.False(result);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EnableEmojiForProjectAsync_AlreadyEnabled_ReturnsTrueWithoutSaving()
    {
        // Arrange
        var emoji = new Emoji { EmojiId = EmojiId, Symbol = "👍", Name = "thumbsup" };
        var project = new Project { ProjectId = ProjectId, Emojis = new List<Emoji> { emoji } };
        _projectRepository.Setup(r => r.ReadByIdWithEmojisForUpdateAsync(ProjectId))
            .ReturnsAsync(project);
        _emojiManager.Setup(r => r.GetByIdForUpdateAsync(EmojiId))
            .ReturnsAsync(emoji);

        // Act
        var result = await _manager.EnableEmojiForProjectAsync(ProjectId, EmojiId);

        // Assert
        Assert.True(result);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _projectRepository.Verify(r => r.Update(It.IsAny<Project>()), Times.Never);
    }

    #endregion

    #region DuplicateProjectAsync

    [Fact]
    public async Task DuplicateProjectAsync_HappyPath_DelegatesToDuplicationServiceAndPersists()
    {
        // Manager-level concern is orchestration: resolve source, validate, normalise the short-form,
        // hand the validated inputs to IProjectDuplicationService, persist the result. The deep-clone
        // graph itself is covered by ProjectDuplicationServiceTests.
        const int srcId = 10;
        const int platformId = 5;

        var source = new Project
        {
            ProjectId = srcId,
            ProjectPlatformId = platformId,
            ShortForm = "source",
            DefaultLanguage = LanguageNl,
            SupportedLanguages = new List<Language> { LanguageNl, LanguageEn },
        };
        var built = new Project { ShortForm = "new-project" };

        _projectRepository.Setup(r => r.ReadByIdForDuplicationReadOnlyAsync(srcId)).ReturnsAsync(source);
        _shortFormService.Setup(s => s.Normalize("New Project")).Returns("new-project");
        _shortFormService.Setup(s => s.IsValid("new-project")).Returns(true);
        _shortFormService.Setup(s => s.IsReserved("new-project")).Returns(false);
        _shortFormService.Setup(s => s.EnsureUniqueAsync("new-project", null)).ReturnsAsync("new-project");
        _duplicationService
            .Setup(s => s.BuildCloneAsync(
                source,
                It.IsAny<IReadOnlyList<ProjectTranslationInput>>(),
                It.IsAny<IReadOnlyDictionary<string, Language>>(),
                "new-project",
                platformId))
            .ReturnsAsync(built)
            .Verifiable(Times.Exactly(1));
        _projectRepository.Setup(r => r.CreateAsync(built)).ReturnsAsync(built).Verifiable(Times.Exactly(1));
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1).Verifiable(Times.Exactly(1));

        var result = await _manager.DuplicateProjectAsync(srcId,
            Translations(("nl", "New Project"), ("en", "New Project EN")),
            platformId);

        Assert.Same(built, result);
        _duplicationService.VerifyAll();
        _projectRepository.Verify(r => r.CreateAsync(built), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DuplicateProjectAsync_CrossTenantSource_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var source = new Project { ProjectId = 1, ProjectPlatformId = 99 };
        _projectRepository.Setup(r => r.ReadByIdForDuplicationReadOnlyAsync(1)).ReturnsAsync(source);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _manager.DuplicateProjectAsync(1, Translations(("nl", "X")), platformId: 7));
    }

    [Fact]
    public async Task DuplicateProjectAsync_SourceNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _projectRepository.Setup(r => r.ReadByIdForDuplicationReadOnlyAsync(999)).ReturnsAsync((Project)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _manager.DuplicateProjectAsync(999, Translations(("nl", "X")), platformId: 1));
    }

    #endregion

    #region UpdateLastWizardStepAsync

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task UpdateLastWizardStepAsync_ValidStep_SetsStepAndSaves(int step)
    {
        // Arrange
        var project = new Project { ProjectId = ProjectId, LastWizardStep = 1 };
        _projectRepository.Setup(r => r.ReadByIdForUpdateAsync(ProjectId)).ReturnsAsync(project);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        await _manager.UpdateLastWizardStepAsync(ProjectId, step);

        // Assert
        Assert.Equal(step, project.LastWizardStep);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(4, 3)]
    [InlineData(99, 3)]
    public async Task UpdateLastWizardStepAsync_OutOfRange_ClampsTo1Through3(int input, int expected)
    {
        // Arrange
        var project = new Project { ProjectId = ProjectId, LastWizardStep = 1 };
        _projectRepository.Setup(r => r.ReadByIdForUpdateAsync(ProjectId)).ReturnsAsync(project);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        await _manager.UpdateLastWizardStepAsync(ProjectId, input);

        // Assert
        Assert.Equal(expected, project.LastWizardStep);
    }

    [Fact]
    public async Task UpdateLastWizardStepAsync_ProjectNotFound_Throws()
    {
        // Arrange
        _projectRepository.Setup(r => r.ReadByIdForUpdateAsync(999)).ReturnsAsync((Project)null);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _manager.UpdateLastWizardStepAsync(999, 2));
    }

    #endregion

    #region DisableEmojiForProjectAsync

    [Fact]
    public async Task DisableEmojiForProjectAsync_HappyPath_RemovesEmojiAndSavesOnce()
    {
        // Arrange
        var emoji = new Emoji { EmojiId = EmojiId, Symbol = "👍", Name = "thumbsup" };
        var project = new Project { ProjectId = ProjectId, Emojis = new List<Emoji> { emoji } };
        _projectRepository.Setup(r => r.ReadByIdWithEmojisForUpdateAsync(ProjectId))
            .ReturnsAsync(project);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .Verifiable(Times.Exactly(1));

        // Act
        var result = await _manager.DisableEmojiForProjectAsync(ProjectId, EmojiId);

        // Assert
        Assert.True(result);
        Assert.DoesNotContain(emoji, project.Emojis);
        _projectRepository.VerifyAll();
        _uow.VerifyAll();
    }

    [Fact]
    public async Task DisableEmojiForProjectAsync_ProjectNotFound_ReturnsFalseWithoutSaving()
    {
        // Arrange
        _projectRepository.Setup(r => r.ReadByIdWithEmojisForUpdateAsync(ProjectId))
            .ReturnsAsync((Project)null);

        // Act
        var result = await _manager.DisableEmojiForProjectAsync(ProjectId, EmojiId);

        // Assert
        Assert.False(result);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DisableEmojiForProjectAsync_EmojiNotEnabled_ReturnsFalseWithoutSaving()
    {
        // Arrange
        _projectRepository.Setup(r => r.ReadByIdWithEmojisForUpdateAsync(ProjectId))
            .ReturnsAsync(new Project { ProjectId = ProjectId, Emojis = new List<Emoji>() });

        // Act
        var result = await _manager.DisableEmojiForProjectAsync(ProjectId, EmojiId);

        // Assert
        Assert.False(result);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _projectRepository.Verify(r => r.Update(It.IsAny<Project>()), Times.Never);
    }

    #endregion
}
