namespace SynapseEnqueteManagement.BL.Tests.Unit;

public class QuestionnaireManagerTests
{
    private readonly Mock<IQuestionnaireRepository> _questionnaireRepo = new(MockBehavior.Strict);
    private readonly Mock<IQuestionRepository> _questionRepo = new(MockBehavior.Strict);
    private readonly Mock<ISectionRepository> _sectionRepo = new(MockBehavior.Strict);
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
            _uow.Object);
    }

    [Fact]
    public async Task AddQuestionAsync_PersistsMediaFields()
    {
        // Arrange
        _questionnaireRepo.Setup(r => r.ReadByProjectIdForUpdateAsync(ProjectId))
            .ReturnsAsync(_questionnaire);
        _questionRepo.Setup(r => r.ReadSiblingsForUpdateAsync(null, null, _questionnaire.QuestionnaireId))
            .ReturnsAsync(new List<Question>());
        _questionRepo.Setup(r => r.CreateAsync(It.IsAny<Question>()))
            .ReturnsAsync((Question q) => q);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _manager.AddQuestionAsync(
            ProjectId, "Tekst", QuestionFormat.Open, false, new List<string>(),
            mediaUrl: "https://storage.googleapis.com/bucket/questions/images/img.jpg",
            mediaType: QuestionMediaType.Image,
            mediaIsEmbed: false,
            mediaText: "**Tip:** kijk goed.");

        // Assert
        Assert.Equal(QuestionMediaType.Image, result.MediaType);
        Assert.Equal("https://storage.googleapis.com/bucket/questions/images/img.jpg", result.MediaUrl);
        Assert.False(result.MediaIsEmbed);
        Assert.Equal("**Tip:** kijk goed.", result.MediaText);
    }

    [Fact]
    public async Task UpdateQuestionAsync_ChangingMediaFromUploadedToEmbed_PreservesNewUrlAndType()
    {
        // Arrange
        var question = new Question
        {
            QuestionId = QuestionId,
            Text = "old",
            QuestionFormat = QuestionFormat.Open,
            MediaUrl = "https://old.example.com/vid.mp4",
            MediaType = QuestionMediaType.Video,
            MediaIsEmbed = false,
            AnswerChoices = new List<string>(),
            EndingAnswers = new List<EndingAnswer>(),
            Children = new List<Question>()
        };

        _questionRepo.Setup(r => r.ReadByIdWithSectionForUpdateAsync(QuestionId))
            .ReturnsAsync(question);
        _questionRepo.Setup(r => r.Update(It.IsAny<Question>()));
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _manager.UpdateQuestionAsync(
            QuestionId, "new text", QuestionFormat.Open, false, new List<string>(),
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
        var question = new Question
        {
            QuestionId = QuestionId,
            Text = "q",
            QuestionFormat = QuestionFormat.Open,
            MediaUrl = "https://old.example.com/img.jpg",
            MediaType = QuestionMediaType.Image,
            MediaIsEmbed = false,
            MediaText = "some text",
            AnswerChoices = new List<string>(),
            EndingAnswers = new List<EndingAnswer>(),
            Children = new List<Question>()
        };

        _questionRepo.Setup(r => r.ReadByIdWithSectionForUpdateAsync(QuestionId))
            .ReturnsAsync(question);
        _questionRepo.Setup(r => r.Update(It.IsAny<Question>()));
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        await _manager.UpdateQuestionAsync(
            QuestionId, "q", QuestionFormat.Open, false, new List<string>(),
            mediaUrl: null, mediaType: null, mediaIsEmbed: false, mediaText: null);

        // Assert
        Assert.Null(question.MediaUrl);
        Assert.Null(question.MediaType);
        Assert.False(question.MediaIsEmbed);
        Assert.Null(question.MediaText);
    }
}
