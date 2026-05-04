using System.ComponentModel.DataAnnotations;
using SynapseEnqueteManagement.BL.Implementation;

namespace SynapseEnqueteManagement.BL.Tests.Unit;

public class ProjectManagerTests
{
    private readonly Mock<IProjectRepository> _projectRepository = new(MockBehavior.Strict);
    private readonly Mock<IEmojiRepository> _emojiRepository = new(MockBehavior.Strict);
    private readonly Mock<IShortFormService> _shortFormService = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _uow = new(MockBehavior.Strict);
    private readonly ProjectManager _manager;

    private const int ProjectId = 1;
    private const int EmojiId = 99;

    public ProjectManagerTests()
    {
        _manager = new ProjectManager(
            _projectRepository.Object,
            _emojiRepository.Object,
            _shortFormService.Object,
            _uow.Object);
    }

    #region CreateProjectAsync

    [Fact]
    public async Task CreateProjectAsync_BlankShortForm_AutofillsFromName()
    {
        // Arrange
        _shortFormService.Setup(s => s.Normalize("My Project")).Returns("my-project");
        _shortFormService.Setup(s => s.IsValid("my-project")).Returns(true);
        _shortFormService.Setup(s => s.IsReserved("my-project")).Returns(false);
        _shortFormService.Setup(s => s.EnsureUniqueAsync("my-project", null)).ReturnsAsync("my-project");
        _projectRepository.Setup(r => r.CreateAsync(It.IsAny<Project>()))
            .ReturnsAsync((Project p) => p);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _manager.CreateProjectAsync(
            "My Project", string.Empty, string.Empty, string.Empty,
            FormType.VerticalScroll, AiAssistanceType.ProActive,
            null, null, false, platformId: 1, shortForm: null);

        // Assert
        Assert.Equal("my-project", result.ShortForm);
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
                "Admin", string.Empty, string.Empty, string.Empty,
                FormType.VerticalScroll, AiAssistanceType.ProActive,
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
                "Test", string.Empty, string.Empty, string.Empty,
                FormType.VerticalScroll, AiAssistanceType.ProActive,
                null, null, false, platformId: 1, shortForm: "INVALID FORM"));
    }

    #endregion

    #region UpdateProjectAsync

    [Fact]
    public async Task UpdateProjectAsync_ShortFormChangeInDraft_UpdatesShortForm()
    {
        // Arrange
        var project = new Project
        {
            ProjectId = ProjectId,
            ShortForm = "old-form",
            ProjectState = ProjectState.Draft
        };
        _projectRepository.Setup(r => r.ReadByIdWithIntroductionForUpdateAsync(ProjectId))
            .ReturnsAsync(project);
        _shortFormService.Setup(s => s.IsValid("new-form")).Returns(true);
        _shortFormService.Setup(s => s.IsReserved("new-form")).Returns(false);
        _shortFormService.Setup(s => s.EnsureUniqueAsync("new-form", ProjectId)).ReturnsAsync("new-form");
        _projectRepository.Setup(r => r.Update(project));
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _manager.UpdateProjectAsync(
            ProjectId, "Name", string.Empty, string.Empty, string.Empty,
            FormType.VerticalScroll, AiAssistanceType.ProActive,
            null, null, false, shortForm: "new-form");

        // Assert
        Assert.Equal("new-form", result.ShortForm);
    }

    [Fact]
    public async Task UpdateProjectAsync_ShortFormChangeInActive_ThrowsValidationException()
    {
        // Arrange
        var project = new Project
        {
            ProjectId = ProjectId,
            ShortForm = "old-form",
            ProjectState = ProjectState.Active
        };
        _projectRepository.Setup(r => r.ReadByIdWithIntroductionForUpdateAsync(ProjectId))
            .ReturnsAsync(project);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _manager.UpdateProjectAsync(
                ProjectId, "Name", string.Empty, string.Empty, string.Empty,
                FormType.VerticalScroll, AiAssistanceType.ProActive,
                null, null, false, shortForm: "changed-form"));
    }

    [Fact]
    public async Task UpdateProjectAsync_SameShortFormInActive_DoesNotThrow()
    {
        // Arrange
        var project = new Project
        {
            ProjectId = ProjectId,
            ShortForm = "same-form",
            ProjectState = ProjectState.Active
        };
        _projectRepository.Setup(r => r.ReadByIdWithIntroductionForUpdateAsync(ProjectId))
            .ReturnsAsync(project);
        _projectRepository.Setup(r => r.Update(project));
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act — passing the same shortform should not throw
        var result = await _manager.UpdateProjectAsync(
            ProjectId, "Name", string.Empty, string.Empty, string.Empty,
            FormType.VerticalScroll, AiAssistanceType.ProActive,
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
    public async Task DuplicateProjectAsync_HappyPath_CopiesGraphAndRemapsParentQuestion()
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
        var srcCat   = new Category { CategoryId = 9, Name = "Cat A", Description = "cat desc" };

        var source = new Project
        {
            ProjectId = srcId,
            ProjectPlatformId = platformId,
            Name = "Source",
            ShortForm = "source",
            Topic = "T",
            ImageUrl = "img.png",
            FormType = FormType.VerticalScroll,
            AiAssistance = AiAssistanceType.ProActive,
            IdeaPageSize = 5,
            MaxExtraIdeaRequests = 3,
            CommentPageSize = 20,
            Introduction = new Introduction { Text = "Intro text" },
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
        var result = await _manager.DuplicateProjectAsync(srcId, "New Project", platformId);

        // Assert
        Assert.Equal("New Project", result.Name);
        Assert.Equal("new-project", result.ShortForm);
        Assert.Equal(ProjectState.Draft, result.ProjectState);
        Assert.Equal(0, result.ViewCount);
        Assert.Equal("Intro text", result.Introduction.Text);

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
        Assert.Equal("Cat A", result.Categories.First().Name);

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
            _manager.DuplicateProjectAsync(1, "X", platformId: 7));
    }

    [Fact]
    public async Task DuplicateProjectAsync_SourceNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _projectRepository.Setup(r => r.ReadByIdForDuplicationReadOnlyAsync(999)).ReturnsAsync((Project)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _manager.DuplicateProjectAsync(999, "X", platformId: 1));
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
