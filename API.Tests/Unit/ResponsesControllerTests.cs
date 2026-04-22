using SynapseEnqueteManagement.API.DTOs.Project;
using SynapseEnqueteManagement.API.DTOs.Survey;

namespace SynapseEnqueteManagement.API.Tests;

public class ResponsesControllerTests
{
    private readonly Mock<IQuestionnaireManager> _questionnaireManager = new();
    private readonly Mock<IParticipantManager> _participantManager = new();
    private readonly Mock<IResponseManager> _responseManager = new();
    private readonly ResponsesController _controller;

    private const int ProjectId = 1;
    private readonly Guid _sessionToken = Guid.NewGuid();
    private readonly Participant _participant;

    public ResponsesControllerTests()
    {
        _participant = new Participant { ParticipantId = 5, SessionToken = Guid.NewGuid(), Project = new Project { ProjectId = ProjectId } };
        _participantManager.Setup(p => p.GetParticipantBySessionTokenAsync(_sessionToken))
            .ReturnsAsync(_participant);
        _controller = new ResponsesController(_questionnaireManager.Object, _participantManager.Object, _responseManager.Object);
        _controller.AttachHttpContext(_sessionToken);
    }

    [Fact]
    public async Task CreateResponse_ForwardsCancellationTokenToResponseManager()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;
        var question = new Question { QuestionId = 7 };
        _questionnaireManager.Setup(q => q.GetQuestionByIdAsync(7)).ReturnsAsync(question);
        _responseManager.Setup(r => r.UpdateOrCreateResponseAsync("val", question, _participant, ct))
            .ReturnsAsync(new Response { ResponseId = 1, Value = "val", Question = question, Participant = _participant })
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
        var questionnaire = new Questionnaire { QuestionnaireId = 1 };
        _questionnaireManager.Setup(q => q.GetQuestionnaireByProjectIdAsync(ProjectId, false)).ReturnsAsync(questionnaire);
        _participantManager.Setup(p => p.AddParticipantAsync(questionnaire, ProjectId, ct))
            .ReturnsAsync(new Participant { SessionToken = Guid.NewGuid() })
            .Verifiable(Times.Exactly(1));

        // Act
        await _controller.StartSession(ProjectId, ct);

        // Assert
        _participantManager.Verify(p => p.AddParticipantAsync(questionnaire, ProjectId, ct), Times.Once);
    }

    [Fact]
    public async Task Submit_ForwardsCancellationTokenToResponseAndParticipantManagers()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;
        var questionnaire = new Questionnaire { QuestionnaireId = 1 };
        _questionnaireManager.Setup(q => q.GetQuestionnaireByProjectIdAsync(ProjectId, true)).ReturnsAsync(questionnaire);
        _questionnaireManager.Setup(q => q.ComputeVisiblePathAsync(ProjectId, _participant.ParticipantId))
            .ReturnsAsync(new List<int> { 1 });
        _responseManager.Setup(r => r.GetResponsesForParticipantAsync(_participant.ParticipantId))
            .ReturnsAsync(new List<Response>());
        _responseManager.Setup(r => r.CommitResponsesAsync(_participant.ParticipantId, It.IsAny<HashSet<int>>(), ct))
            .Returns(Task.CompletedTask)
            .Verifiable(Times.Exactly(1));
        _participantManager.Setup(p => p.MarkAsSubmittedAsync(_participant.SessionToken, ct))
            .Returns(Task.CompletedTask)
            .Verifiable(Times.Exactly(1));

        // Act
        await _controller.Submit(ProjectId, ct);

        // Assert
        _responseManager.VerifyAll();
        _participantManager.Verify(p => p.MarkAsSubmittedAsync(_participant.SessionToken, ct), Times.Once);
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
}
