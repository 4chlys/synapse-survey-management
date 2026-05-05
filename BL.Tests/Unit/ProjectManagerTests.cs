using System.ComponentModel.DataAnnotations;
using SynapseEnqueteManagement.BL.Domain.I18n.Entities;
using SynapseEnqueteManagement.BL.Implementation;

namespace SynapseEnqueteManagement.BL.Tests.Unit;

public class ProjectManagerTests
{
    private readonly Mock<IProjectRepository> _projectRepository = new(MockBehavior.Strict);
    private readonly Mock<IEmojiRepository> _emojiRepository = new(MockBehavior.Strict);
    private readonly Mock<ILanguageRepository> _languageRepository = new(MockBehavior.Strict);
    private readonly Mock<IShortFormService> _shortFormService = new(MockBehavior.Strict);
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
            _emojiRepository.Object,
            _languageRepository.Object,
            _shortFormService.Object,
            _uow.Object);

        _languageRepository
            .Setup(r => r.ReadByCodesForUpdateAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync((IEnumerable<string> codes) =>
            {
                var set = codes.ToHashSet();
                return new[] { LanguageNl, LanguageEn }.Where(l => set.Contains(l.Code)).ToList();
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
            string.Empty, FormType.VerticalScroll, AiAssistanceType.ProActive,
            null, null, false, platformId: 1, shortForm: null);

        // Assert
        Assert.Equal("my-project", result.ShortForm);
        Assert.Equal("en", result.DefaultLanguage.Code);
        Assert.Equal(2, result.Translations.Count);
    }

    [Fact]
    public async Task CreateProjectAsync_ReservedShortForm_ThrowsValidationException()
    {
        // Arrange
        _shortFormService.Setup(s => s.IsValid("admin")).Returns(true);
        _shortFormService.Setup(s => s.IsReserved("admin")).Returns(true);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _manager.CreateProjectAsync(
                Translations(("nl", "Admin")),
                NoIntroductions(),
                "nl", new[] { "nl" },
                string.Empty, FormType.VerticalScroll, AiAssistanceType.ProActive,
                null, null, false, platformId: 1, shortForm: "admin"));
    }

    [Fact]
    public async Task CreateProjectAsync_InvalidShortForm_ThrowsValidationException()
    {
        // Arrange
        _shortFormService.Setup(s => s.IsValid("INVALID FORM")).Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _manager.CreateProjectAsync(
                Translations(("nl", "Test")),
                NoIntroductions(),
                "nl", new[] { "nl" },
                string.Empty, FormType.VerticalScroll, AiAssistanceType.ProActive,
                null, null, false, platformId: 1, shortForm: "INVALID FORM"));
    }

    [Fact]
    public async Task CreateProjectAsync_DefaultLanguageNotInSupported_Throws()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _manager.CreateProjectAsync(
                Translations(("nl", "X")),
                NoIntroductions(),
                defaultLanguageCode: "en",
                supportedLanguageCodes: new[] { "nl" },
                string.Empty, FormType.VerticalScroll, AiAssistanceType.ProActive,
                null, null, false, platformId: 1));
    }

    [Fact]
    public async Task CreateProjectAsync_MissingTranslationForSupportedLanguage_Throws()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _manager.CreateProjectAsync(
                Translations(("nl", "X")),
                NoIntroductions(),
                "nl", new[] { "nl", "en" },
                string.Empty, FormType.VerticalScroll, AiAssistanceType.ProActive,
                null, null, false, platformId: 1));
    }

    [Fact]
    public async Task CreateProjectAsync_UnknownLanguageCode_Throws()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _manager.CreateProjectAsync(
                Translations(("zz", "X")),
                NoIntroductions(),
                "zz", new[] { "zz" },
                string.Empty, FormType.VerticalScroll, AiAssistanceType.ProActive,
                null, null, false, platformId: 1));
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
            string.Empty, FormType.VerticalScroll, AiAssistanceType.ProActive,
            null, null, false, shortForm: "new-form");

        // Assert
        Assert.Equal("new-form", result.ShortForm);
    }

    [Fact]
    public async Task UpdateProjectAsync_ShortFormChangeInActive_ThrowsValidationException()
    {
        // Arrange
        var project = SeedExistingProject(ProjectState.Active);
        _projectRepository.Setup(r => r.ReadByIdWithTranslationsAndLanguagesForUpdateAsync(ProjectId))
            .ReturnsAsync(project);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _manager.UpdateProjectAsync(
                ProjectId,
                Translations(("nl", "Naam")),
                NoIntroductions(),
                "nl", new[] { "nl" },
                string.Empty, FormType.VerticalScroll, AiAssistanceType.ProActive,
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
            string.Empty, FormType.VerticalScroll, AiAssistanceType.ProActive,
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
        _emojiRepository.Setup(r => r.ReadByIdForUpdateAsync(EmojiId))
            .ReturnsAsync(emoji);
        _projectRepository.Setup(r => r.Update(project)).Verifiable(Times.Exactly(1));
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
        _emojiRepository.Setup(r => r.ReadByIdForUpdateAsync(EmojiId))
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
        _emojiRepository.Setup(r => r.ReadByIdForUpdateAsync(EmojiId))
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
        _emojiRepository.Setup(r => r.ReadByIdForUpdateAsync(EmojiId))
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
    public async Task DuplicateProjectAsync_HappyPath_CopiesGraphAndTranslations()
    {
        // Arrange
        const int srcId = 10;
        const int platformId = 5;
        const int emojiId = 42;

        var srcSection = new Section { SectionId = 1, Name = "Sectie 1", Description = "desc" };
        var srcParent = new Question { QuestionId = 101, Text = "Parent?", QuestionFormat = QuestionFormat.Open, IsMandatory = true, AnswerChoices = new List<string>(), Position = 1 };
        var srcChild  = new Question { QuestionId = 102, Text = "Child?",  QuestionFormat = QuestionFormat.MultipleChoice, IsMandatory = false, AnswerChoices = new List<string> { "A", "B" }, Position = 2, Section = srcSection, ParentQuestion = srcParent, ParentTriggerValue = "yes" };
        srcChild.EndingAnswers.Add(new EndingAnswer { Value = "end" });

        var srcEmoji = new Emoji { EmojiId = emojiId, Symbol = "❤️", Name = "heart" };
        var srcCat   = new Category
        {
            CategoryId = 9,
            Translations = new List<CategoryTranslation>
            {
                new() { Language = LanguageNl, Name = "Cat A", Description = "cat desc" },
            }
        };

        var srcNlTrans = new ProjectTranslation { Language = LanguageNl, Name = "Source", Topic = "T-nl" };
        var srcEnTrans = new ProjectTranslation { Language = LanguageEn, Name = "Source EN", Topic = "T-en" };

        var source = new Project
        {
            ProjectId = srcId,
            ProjectPlatformId = platformId,
            ShortForm = "source",
            ImageUrl = "img.png",
            FormType = FormType.VerticalScroll,
            AiAssistance = AiAssistanceType.ProActive,
            IdeaPageSize = 5,
            MaxExtraIdeaRequests = 3,
            CommentPageSize = 20,
            DefaultLanguage = LanguageNl,
            SupportedLanguages = new List<Language> { LanguageNl, LanguageEn },
            Translations = new List<ProjectTranslation> { srcNlTrans, srcEnTrans },
            Introduction = new Introduction
            {
                Translations = new List<IntroductionTranslation>
                {
                    new() { Language = LanguageNl, Text = "Intro text" },
                }
            },
            Questionnaire = new Questionnaire
            {
                Sections = new List<Section> { srcSection },
                Questions = new List<Question> { srcParent, srcChild },
            },
            Categories = new List<Category> { srcCat },
            Emojis = new List<Emoji> { srcEmoji },
        };

        var trackedEmoji = new Emoji { EmojiId = emojiId, Symbol = "❤️", Name = "heart" };

        _projectRepository.Setup(r => r.ReadByIdForDuplicationReadOnlyAsync(srcId)).ReturnsAsync(source);
        _shortFormService.Setup(s => s.Normalize("New Project")).Returns("new-project");
        _shortFormService.Setup(s => s.IsValid("new-project")).Returns(true);
        _shortFormService.Setup(s => s.IsReserved("new-project")).Returns(false);
        _shortFormService.Setup(s => s.EnsureUniqueAsync("new-project", null)).ReturnsAsync("new-project");
        _emojiRepository.Setup(r => r.ReadByIdForUpdateAsync(emojiId)).ReturnsAsync(trackedEmoji);
        _projectRepository.Setup(r => r.CreateAsync(It.IsAny<Project>())).ReturnsAsync((Project p) => p);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _manager.DuplicateProjectAsync(srcId,
            Translations(("nl", "New Project"), ("en", "New Project EN")),
            platformId);

        // Assert
        Assert.Equal("new-project", result.ShortForm);
        Assert.Equal(ProjectState.Draft, result.ProjectState);
        Assert.Equal(0, result.ViewCount);
        Assert.NotNull(result.Introduction);
        Assert.Single(result.Introduction.Translations);
        Assert.Equal("Intro text", result.Introduction.Translations.First().Text);

        Assert.Equal(2, result.Translations.Count);
        var nlClone = result.Translations.First(t => t.Language.Code == "nl");
        var enClone = result.Translations.First(t => t.Language.Code == "en");
        Assert.Equal("New Project", nlClone.Name);
        Assert.Equal("New Project EN", enClone.Name);

        Assert.Single(result.Questionnaire.Sections);
        Assert.Equal("Sectie 1", result.Questionnaire.Sections.First().Name);

        Assert.Equal(2, result.Questionnaire.Questions.Count);
        var newParent = result.Questionnaire.Questions.First(q => q.Position == 1);
        var newChild  = result.Questionnaire.Questions.First(q => q.Position == 2);

        // Parent-child remap: child's ParentQuestion must point to the NEW parent, not the source
        Assert.Same(newParent, newChild.ParentQuestion);
        Assert.NotSame(srcParent, newChild.ParentQuestion);

        Assert.Equal("yes", newChild.ParentTriggerValue);
        Assert.Single(newChild.EndingAnswers);
        Assert.Equal("end", newChild.EndingAnswers.First().Value);
        Assert.Same(result.Questionnaire.Sections.First(), newChild.Section);

        Assert.Single(result.Categories);
        Assert.Single(result.Categories.First().Translations);
        Assert.Equal("Cat A", result.Categories.First().Translations.First().Name);

        Assert.Single(result.Emojis);
        Assert.Same(trackedEmoji, result.Emojis.First());
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

    #region DisableEmojiForProjectAsync

    [Fact]
    public async Task DisableEmojiForProjectAsync_HappyPath_RemovesEmojiAndSavesOnce()
    {
        // Arrange
        var emoji = new Emoji { EmojiId = EmojiId, Symbol = "👍", Name = "thumbsup" };
        var project = new Project { ProjectId = ProjectId, Emojis = new List<Emoji> { emoji } };
        _projectRepository.Setup(r => r.ReadByIdWithEmojisForUpdateAsync(ProjectId))
            .ReturnsAsync(project);
        _projectRepository.Setup(r => r.Update(project)).Verifiable(Times.Exactly(1));
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
