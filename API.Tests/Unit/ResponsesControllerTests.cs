using SynapseEnqueteManagement.API.DTOs.Project;
using SynapseEnqueteManagement.API.DTOs.Survey;

namespace SynapseEnqueteManagement.API.Tests;

public class ResponsesControllerTests
{
    private readonly Mock<IParticipantManager> _participantManager = new();
    private readonly Mock<IResponseManager> _responseManager = new();
    private readonly Mock<ISubmissionManager> _submissionManager = new();
    private readonly ResponsesController _controller;

    private const int ProjectId = 1;
    private readonly Guid _sessionToken = Guid.NewGuid();
    private readonly Participant _participant;

    public ResponsesControllerTests()
    {
        _participant = new Participant { ParticipantId = 5, SessionToken = Guid.NewGuid(), Project = new Project { ProjectId = ProjectId } };
        _participantManager.Setup(p => p.GetParticipantBySessionTokenAsync(_sessionToken))
            .ReturnsAsync(_participant);
        _controller = new ResponsesController(_participantManager.Object, _responseManager.Object, _submissionManager.Object);
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
        _participantManager.Setup(p => p.AddParticipantAsync(ProjectId, ct))
            .ReturnsAsync(new Participant { SessionToken = Guid.NewGuid() })
            .Verifiable(Times.Exactly(1));

        // Act
        await _controller.StartSession(ProjectId, ct);

        // Assert
        _participantManager.Verify(p => p.AddParticipantAsync(ProjectId, ct), Times.Once);
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
}
