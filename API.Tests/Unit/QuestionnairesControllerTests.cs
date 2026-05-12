using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SynapseEnqueteManagement.API.Controllers;
using SynapseEnqueteManagement.API.DTOs.Survey;
using SynapseEnqueteManagement.API.Services;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.API.Tests;

public class QuestionnairesControllerTests
{
    private readonly Mock<IQuestionnaireManager> _questionnaireManager = new();
    private readonly Mock<ISurveyFlowManager> _surveyFlowManager = new();
    private readonly Mock<IParticipantManager> _participantManager = new();
    private readonly Mock<IProjectManager> _projectManager = new();
    private readonly Mock<IStorageService> _storageService = new();
    private readonly Mock<IWebHostEnvironment> _env = new();
    private readonly QuestionnairesController _controller;

    public QuestionnairesControllerTests()
    {
        // IsDevelopment() is an extension method that reads EnvironmentName — setting this is sufficient
        _env.Setup(e => e.EnvironmentName).Returns("Development");

        _controller = new QuestionnairesController(
            _questionnaireManager.Object,
            _surveyFlowManager.Object,
            _participantManager.Object,
            _projectManager.Object,
            _storageService.Object,
            _env.Object);

        // Provide a real ServiceProvider so the catch-path logger lookup doesn't NRE
        var sp = new ServiceCollection().AddLogging().BuildServiceProvider();
        _controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new DefaultHttpContext { RequestServices = sp }
        };
    }

    [Fact]
    public async Task AddQuestion_ImageEmbedHttpsUrl_PassesValidation()
    {
        var dto = new CreateQuestionDto
        {
            QuestionFormat = "Open",
            AnswerChoiceCount = 0,
            Translations = new List<QuestionTranslationDto>
            {
                new() { LanguageCode = "nl", Text = "Question", AnswerChoices = new List<string>() }
            },
            MediaUrl = "https://example.com/img.jpg",
            MediaType = BL.Domain.Project.Enums.QuestionMediaType.Image,
            MediaIsEmbed = true
        };

        _questionnaireManager
            .Setup(m => m.AddQuestionAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<BL.Domain.Project.Enums.QuestionFormat>(),
                It.IsAny<bool>(), It.IsAny<IEnumerable<QuestionTranslationInput>>(), It.IsAny<int?>(),
                It.IsAny<int?>(), It.IsAny<int?>(),
                It.IsAny<IEnumerable<IEnumerable<EndingAnswerTranslationInput>>>(),
                It.IsAny<string>(), It.IsAny<BL.Domain.Project.Enums.QuestionMediaType?>(),
                It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BL.Domain.Project.Entities.Question
            {
                QuestionId = 1,
                QuestionFormat = BL.Domain.Project.Enums.QuestionFormat.Open,
                AnswerChoiceCount = 0,
                Translations = new List<BL.Domain.Project.Entities.QuestionTranslation>(),
                EndingAnswers = new List<BL.Domain.Project.Entities.EndingAnswer>()
            });

        var result = await _controller.AddQuestion(1, dto, CancellationToken.None);

        Assert.IsNotType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task AddQuestion_EmbedVideoUnknownHostNoExtension_ReturnsBadRequest()
    {
        var dto = new CreateQuestionDto
        {
            QuestionFormat = "Open",
            AnswerChoiceCount = 0,
            Translations = new List<QuestionTranslationDto>
            {
                new() { LanguageCode = "nl", Text = "Question", AnswerChoices = new List<string>() }
            },
            MediaUrl = "https://evil.example.com/stream",
            MediaType = BL.Domain.Project.Enums.QuestionMediaType.Video,
            MediaIsEmbed = true
        };

        var result = await _controller.AddQuestion(1, dto, CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("YouTube/Vimeo", bad.Value?.ToString());
    }

    [Fact]
    public async Task AddQuestion_YouTubeEmbed_CanonicalizedToEmbedUrl()
    {
        var dto = new CreateQuestionDto
        {
            QuestionFormat = "Open",
            AnswerChoiceCount = 0,
            Translations = new List<QuestionTranslationDto>
            {
                new() { LanguageCode = "nl", Text = "Question", AnswerChoices = new List<string>() }
            },
            MediaUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
            MediaType = BL.Domain.Project.Enums.QuestionMediaType.Video,
            MediaIsEmbed = true
        };

        var capturedUrl = string.Empty;
        _questionnaireManager
            .Setup(m => m.AddQuestionAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<BL.Domain.Project.Enums.QuestionFormat>(),
                It.IsAny<bool>(), It.IsAny<IEnumerable<QuestionTranslationInput>>(), It.IsAny<int?>(),
                It.IsAny<int?>(), It.IsAny<int?>(),
                It.IsAny<IEnumerable<IEnumerable<EndingAnswerTranslationInput>>>(),
                It.IsAny<string>(), It.IsAny<BL.Domain.Project.Enums.QuestionMediaType?>(),
                It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Callback<int, int, BL.Domain.Project.Enums.QuestionFormat, bool,
                IEnumerable<QuestionTranslationInput>, int?, int?, int?,
                IEnumerable<IEnumerable<EndingAnswerTranslationInput>>,
                string, BL.Domain.Project.Enums.QuestionMediaType?, bool, CancellationToken>(
                (_, _, _, _, _, _, _, _, _, url, _, _, _) => capturedUrl = url)
            .ReturnsAsync(new BL.Domain.Project.Entities.Question
            {
                QuestionId = 1,
                QuestionFormat = BL.Domain.Project.Enums.QuestionFormat.Open,
                AnswerChoiceCount = 0,
                Translations = new List<BL.Domain.Project.Entities.QuestionTranslation>(),
                EndingAnswers = new List<BL.Domain.Project.Entities.EndingAnswer>()
            });

        await _controller.AddQuestion(1, dto, CancellationToken.None);

        Assert.Equal("https://www.youtube.com/embed/dQw4w9WgXcQ", capturedUrl);
    }

    [Fact]
    public async Task UpdateQuestion_OldStorageUrl_DeleteAsyncCalled()
    {
        const int projectId = 1;
        const int questionId = 5;
        var oldUrl = "http://localhost/uploads/questions/images/old.jpg";

        _questionnaireManager.Setup(m => m.QuestionBelongsToProjectAsync(questionId, projectId))
            .ReturnsAsync(true);
        _questionnaireManager.Setup(m => m.GetQuestionByIdReadOnlyAsync(questionId))
            .ReturnsAsync(new BL.Domain.Project.Entities.Question
            {
                QuestionId = questionId,
                QuestionFormat = BL.Domain.Project.Enums.QuestionFormat.Open,
                AnswerChoiceCount = 0,
                MediaUrl = oldUrl,
                MediaType = BL.Domain.Project.Enums.QuestionMediaType.Image,
                MediaIsEmbed = false,
                Translations = new List<BL.Domain.Project.Entities.QuestionTranslation>(),
                EndingAnswers = new List<BL.Domain.Project.Entities.EndingAnswer>()
            });
        _questionnaireManager.Setup(m => m.UpdateQuestionAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<BL.Domain.Project.Enums.QuestionFormat>(),
            It.IsAny<bool>(), It.IsAny<IEnumerable<QuestionTranslationInput>>(), It.IsAny<int?>(),
            It.IsAny<IEnumerable<IEnumerable<EndingAnswerTranslationInput>>>(),
            It.IsAny<string>(), It.IsAny<BL.Domain.Project.Enums.QuestionMediaType?>(),
            It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _storageService.Setup(s => s.DeleteAsync(oldUrl)).Returns(Task.CompletedTask);

        var dto = new UpdateQuestionDto
        {
            QuestionFormat = "Open",
            AnswerChoiceCount = 0,
            Translations = new List<QuestionTranslationDto>
            {
                new() { LanguageCode = "nl", Text = "q", AnswerChoices = new List<string>() }
            },
            MediaUrl = "http://localhost/uploads/questions/images/new.jpg",
            MediaType = BL.Domain.Project.Enums.QuestionMediaType.Image,
            MediaIsEmbed = false
        };

        await _controller.UpdateQuestion(projectId, questionId, dto, CancellationToken.None);

        _storageService.Verify(s => s.DeleteAsync(oldUrl), Times.Once);
    }
}
