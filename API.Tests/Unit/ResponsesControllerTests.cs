using SynapseEnqueteManagement.API.DTOs.Participants;
using SynapseEnqueteManagement.API.DTOs.Project;
using SynapseEnqueteManagement.API.DTOs.Survey;

namespace SynapseEnqueteManagement.API.Tests;

public class ResponsesControllerTests
{
    private readonly Mock<IParticipantManager> _participantManager = new();
    private readonly Mock<IResponseManager> _responseManager = new();
    private readonly Mock<ISubmissionManager> _submissionManager = new();
    private readonly Mock<IProjectManager> _projectManager = new();
    private readonly Mock<IParticipantLanguageCoordinator> _participantLanguageCoordinator = new();
    private readonly ResponsesController _controller;

    private const int ProjectId = 1;
    private readonly Guid _sessionToken = Guid.NewGuid();
    private readonly Participant _participant;

    public ResponsesControllerTests()
    {
        _participant = new Participant { ParticipantId = 5, SessionToken = Guid.NewGuid(), Project = new Project { ProjectId = ProjectId } };
        _participantManager.Setup(p => p.GetParticipantBySessionTokenForUpdateAsync(_sessionToken))
            .ReturnsAsync(_participant);
        _controller = new ResponsesController(_participantManager.Object, _responseManager.Object, _submissionManager.Object, _projectManager.Object, _participantLanguageCoordinator.Object);
        _controller.AttachHttpContext(_sessionToken);
    }

    [Fact]
    public async Task CreateResponse_ForwardsCancellationTokenToResponseManager()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;
        _responseManager.Setup(r => r.UpdateOrCreateResponseAsync(7, "val", _participant, ct))
            .ReturnsAsync(new Response { ResponseId = 1, Value = "val", Question = new Question { QuestionId = 7 }, Participant = _participant })
            .Verifiable(Times.Exactly(1));

        // Act
        await _controller.CreateResponse(ProjectId, new CreateResponseDto { QuestionId = 7, Value = "val" }, ct);

        // Assert
        _responseManager.VerifyAll();
    }

    [Fact]
    public async Task StartSession_ForwardsCancellationTokenToParticipantManager()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;
        var project = new Project { ProjectId = ProjectId };
        _projectManager.Setup(p => p.GetProjectByIdWithQuestionnaireForUpdateAsync(ProjectId))
            .ReturnsAsync(project);
        _participantManager.Setup(p => p.AddParticipantAsync(project, "nl", ct))
            .ReturnsAsync(new Participant { SessionToken = Guid.NewGuid() })
            .Verifiable(Times.Exactly(1));

        // Act
        await _controller.StartSession(ProjectId, null, ct);

        // Assert
        _participantManager.Verify(p => p.AddParticipantAsync(project, "nl", ct), Times.Once);
    }

    [Fact]
    public async Task Submit_ForwardsCancellationTokenToSubmissionManager()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;
        _submissionManager.Setup(s => s.SubmitAsync(ProjectId, _participant, ct))
            .Returns(Task.CompletedTask)
            .Verifiable(Times.Exactly(1));

        // Act
        await _controller.Submit(ProjectId, ct);

        // Assert
        _submissionManager.VerifyAll();
    }

    [Fact]
    public async Task SaveContactInfo_ForwardsCancellationTokenToParticipantManager()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;
        _participantManager.Setup(p => p.SaveContactInfoAsync(_participant.ParticipantId, "a@b.c", ct))
            .Returns(Task.CompletedTask)
            .Verifiable(Times.Exactly(1));

        // Act
        await _controller.SaveContactInfo(ProjectId, new SaveContactInfoDto { Email = "a@b.c" }, ct);

        // Assert
        _participantManager.Verify(p => p.SaveContactInfoAsync(_participant.ParticipantId, "a@b.c", ct), Times.Once);
    }

    [Fact]
    public async Task SetLanguage_KnownActiveCode_Returns204AndCallsCoordinator()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;
        _participantLanguageCoordinator.Setup(c => c.SwitchLanguageAsync(_participant.ParticipantId, "en", ct))
            .Returns(Task.CompletedTask)
            .Verifiable(Times.Exactly(1));

        // Act
        var result = await _controller.SetLanguage(ProjectId, new SetParticipantLanguageDto { LanguageCode = "en" }, ct);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _participantLanguageCoordinator.VerifyAll();
    }

    [Fact]
    public async Task SetLanguage_UnknownCode_CoordinatorThrows_Returns400()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;
        _participantLanguageCoordinator.Setup(c => c.SwitchLanguageAsync(_participant.ParticipantId, "xx", ct))
            .ThrowsAsync(new KeyNotFoundException("Language 'xx' is not a supported active language."));

        // Act
        var result = await _controller.SetLanguage(ProjectId, new SetParticipantLanguageDto { LanguageCode = "xx" }, ct);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task SetLanguage_NoTokenHeader_Returns401()
    {
        // Arrange
        var controllerWithoutToken = new ResponsesController(
            _participantManager.Object,
            _responseManager.Object,
            _submissionManager.Object,
            _projectManager.Object,
            _participantLanguageCoordinator.Object);
        controllerWithoutToken.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await controllerWithoutToken.SetLanguage(
            ProjectId,
            new SetParticipantLanguageDto { LanguageCode = "en" },
            CancellationToken.None);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }
}
