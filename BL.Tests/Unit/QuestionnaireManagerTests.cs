using SynapseEnqueteManagement.BL.Domain.I18n.Entities;

namespace SynapseEnqueteManagement.BL.Tests.Unit;

public class QuestionnaireManagerTests
{
    private readonly Mock<IQuestionnaireRepository> _questionnaireRepo = new(MockBehavior.Strict);
    private readonly Mock<IQuestionRepository> _questionRepo = new(MockBehavior.Strict);
    private readonly Mock<ISectionRepository> _sectionRepo = new(MockBehavior.Strict);
    private readonly Mock<ILanguageResolver> _languageResolver = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _uow = new(MockBehavior.Strict);
    private readonly QuestionnaireManager _manager;

    private const int ProjectId = 1;
    private const int QuestionId = 10;

    private readonly Questionnaire _questionnaire = new()
    {
        QuestionnaireId = 99,
        Project = new Project { ProjectId = ProjectId }
    };

    public QuestionnaireManagerTests()
    {
        _manager = new QuestionnaireManager(
            _questionnaireRepo.Object,
            _questionRepo.Object,
            _sectionRepo.Object,
            _languageResolver.Object,
            _uow.Object);
    }

    [Fact]
    public async Task AddQuestionAsync_PersistsMediaFields()
    {
        // Arrange
        var nlLanguage = new Language { Code = "nl" };
        _questionnaireRepo.Setup(r => r.ReadByProjectIdForUpdateAsync(ProjectId))
            .ReturnsAsync(_questionnaire);
        _questionRepo.Setup(r => r.ReadSiblingsForUpdateAsync(null, null, _questionnaire.QuestionnaireId))
            .ReturnsAsync(new List<Question>());
        _questionRepo.Setup(r => r.CreateAsync(It.IsAny<Question>()))
            .ReturnsAsync((Question q) => q);
        _languageResolver.Setup(r => r.ResolveForTranslationsAsync(It.IsAny<ICollection<string>>(), true))
            .ReturnsAsync((IReadOnlyDictionary<string, Language>)new Dictionary<string, Language> { ["nl"] = nlLanguage });
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _manager.AddQuestionAsync(
            ProjectId, 0, QuestionFormat.Open, false,
            new[] { new QuestionTranslationInput("nl", "Tekst", new List<string>(), "**Tip:** kijk goed.") },
            mediaUrl: "https://storage.googleapis.com/bucket/questions/images/img.jpg",
            mediaType: QuestionMediaType.Image,
            mediaIsEmbed: false);

        // Assert
        Assert.Equal(QuestionMediaType.Image, result.MediaType);
        Assert.Equal("https://storage.googleapis.com/bucket/questions/images/img.jpg", result.MediaUrl);
        Assert.False(result.MediaIsEmbed);
        Assert.Equal("**Tip:** kijk goed.", result.Translations.Single().MediaText);
    }

    [Fact]
    public async Task UpdateQuestionAsync_ChangingMediaFromUploadedToEmbed_PreservesNewUrlAndType()
    {
        // Arrange
        var nlLanguage = new Language { Code = "nl" };
        var question = new Question
        {
            QuestionId = QuestionId,
            QuestionFormat = QuestionFormat.Open,
            AnswerChoiceCount = 0,
            MediaUrl = "https://old.example.com/vid.mp4",
            MediaType = QuestionMediaType.Video,
            MediaIsEmbed = false,
            Translations = new List<QuestionTranslation>(),
            EndingAnswers = new List<EndingAnswer>(),
            Children = new List<Question>()
        };

        _questionRepo.Setup(r => r.ReadByIdWithSectionForUpdateAsync(QuestionId))
            .ReturnsAsync(question);
        _languageResolver.Setup(r => r.ResolveForTranslationsAsync(It.IsAny<ICollection<string>>(), true))
            .ReturnsAsync((IReadOnlyDictionary<string, Language>)new Dictionary<string, Language> { ["nl"] = nlLanguage });
        _questionRepo.Setup(r => r.Update(It.IsAny<Question>()));
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _manager.UpdateQuestionAsync(
            QuestionId, 0, QuestionFormat.Open, false,
            new[] { new QuestionTranslationInput("nl", "new text", new List<string>(), null) },
            mediaUrl: "https://www.youtube.com/embed/dQw4w9WgXcQ",
            mediaType: QuestionMediaType.Video,
            mediaIsEmbed: true);

        // Assert
        Assert.True(result);
        Assert.Equal("https://www.youtube.com/embed/dQw4w9WgXcQ", question.MediaUrl);
        Assert.True(question.MediaIsEmbed);
    }

    [Fact]
    public async Task UpdateQuestionAsync_NullMediaType_NullsAllMediaFields()
    {
        // Arrange
        var nlLanguage = new Language { Code = "nl" };
        var question = new Question
        {
            QuestionId = QuestionId,
            QuestionFormat = QuestionFormat.Open,
            AnswerChoiceCount = 0,
            MediaUrl = "https://old.example.com/img.jpg",
            MediaType = QuestionMediaType.Image,
            MediaIsEmbed = false,
            Translations = new List<QuestionTranslation>(),
            EndingAnswers = new List<EndingAnswer>(),
            Children = new List<Question>()
        };

        _questionRepo.Setup(r => r.ReadByIdWithSectionForUpdateAsync(QuestionId))
            .ReturnsAsync(question);
        _languageResolver.Setup(r => r.ResolveForTranslationsAsync(It.IsAny<ICollection<string>>(), true))
            .ReturnsAsync((IReadOnlyDictionary<string, Language>)new Dictionary<string, Language> { ["nl"] = nlLanguage });
        _questionRepo.Setup(r => r.Update(It.IsAny<Question>()));
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        await _manager.UpdateQuestionAsync(
            QuestionId, 0, QuestionFormat.Open, false,
            new[] { new QuestionTranslationInput("nl", "q", new List<string>(), null) },
            mediaUrl: null, mediaType: null, mediaIsEmbed: false);

        // Assert
        Assert.Null(question.MediaUrl);
        Assert.Null(question.MediaType);
        Assert.False(question.MediaIsEmbed);
    }

    #region ValidateMandatoryAnswersAsync

    private static Questionnaire QuestionnaireWith(params Question[] questions)
        => new() { QuestionnaireId = 99, Questions = questions.ToList() };

    private static Question Mandatory(int id) => new() { QuestionId = id, IsMandatory = true };
    private static Question Optional(int id) => new() { QuestionId = id, IsMandatory = false };

    [Fact]
    public async Task ValidateMandatoryAnswersAsync_QuestionnaireNotFound_ThrowsKeyNotFound()
    {
        _questionnaireRepo.Setup(r => r.ReadByProjectIdWithQuestionsReadOnlyAsync(ProjectId))
            .ReturnsAsync((Questionnaire)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _manager.ValidateMandatoryAnswersAsync(ProjectId, Array.Empty<(int, string)>()));
    }

    [Fact]
    public async Task ValidateMandatoryAnswersAsync_AllMandatoryAnswered_DoesNotThrow()
    {
        _questionnaireRepo.Setup(r => r.ReadByProjectIdWithQuestionsReadOnlyAsync(ProjectId))
            .ReturnsAsync(QuestionnaireWith(Mandatory(1), Optional(2)));

        var answers = new[] { (1, "yes"), (2, "") };

        await _manager.ValidateMandatoryAnswersAsync(ProjectId, answers);
    }

    [Fact]
    public async Task ValidateMandatoryAnswersAsync_MissingMandatory_Throws()
    {
        _questionnaireRepo.Setup(r => r.ReadByProjectIdWithQuestionsReadOnlyAsync(ProjectId))
            .ReturnsAsync(QuestionnaireWith(Mandatory(1), Mandatory(2)));

        var answers = new[] { (1, "yes") };

        var ex = await Assert.ThrowsAsync<SynapseEnqueteManagement.BL.Exceptions.LocalizedValidationException>(() =>
            _manager.ValidateMandatoryAnswersAsync(ProjectId, answers));

        Assert.Equal("Validation.Question.MissingMandatoryAnswers", ex.Code);
    }

    [Fact]
    public async Task ValidateMandatoryAnswersAsync_BlankAnswerCountsAsUnanswered_Throws()
    {
        _questionnaireRepo.Setup(r => r.ReadByProjectIdWithQuestionsReadOnlyAsync(ProjectId))
            .ReturnsAsync(QuestionnaireWith(Mandatory(1)));

        var answers = new[] { (1, "   ") };

        await Assert.ThrowsAsync<SynapseEnqueteManagement.BL.Exceptions.LocalizedValidationException>(() =>
            _manager.ValidateMandatoryAnswersAsync(ProjectId, answers));
    }

    [Fact]
    public async Task ValidateMandatoryAnswersAsync_InvisibleMandatorySkipped_DoesNotThrow()
    {
        _questionnaireRepo.Setup(r => r.ReadByProjectIdWithQuestionsReadOnlyAsync(ProjectId))
            .ReturnsAsync(QuestionnaireWith(Mandatory(1), Mandatory(2)));

        // Q2 is mandatory but not on the visible path, so its absence must not fail validation.
        var answers = new[] { (1, "yes") };
        var visible = new HashSet<int> { 1 };

        await _manager.ValidateMandatoryAnswersAsync(ProjectId, answers, visible);
    }

    #endregion
}
