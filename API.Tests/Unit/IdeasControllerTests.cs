using SynapseEnqueteManagement.API.DTOs.Ideation;

namespace SynapseEnqueteManagement.API.Tests;

public class IdeasControllerTests
{
    private readonly Mock<IIdeaManager> _ideaManager = new();
    private readonly Mock<ICommentManager> _commentManager = new();
    private readonly Mock<IProjectManager> _projectManager = new();
    private readonly Mock<IParticipantManager> _participantManager = new();
    private readonly Mock<IModerationResolver> _moderationResolver = new();
    private readonly IdeasController _controller;

    private const int ProjectId = 1;
    private const int CategoryId = 2;
    private readonly Guid _sessionToken = Guid.NewGuid();
    private readonly Participant _participant;

    public IdeasControllerTests()
    {
        _participant = new Participant { ParticipantId = 5, Project = new Project { ProjectId = ProjectId } };
        _participantManager.Setup(p => p.GetParticipantBySessionTokenAsync(_sessionToken))
            .ReturnsAsync(_participant);

        _controller = new IdeasController(
            _ideaManager.Object,
            _commentManager.Object,
            _projectManager.Object,
            _participantManager.Object,
            _moderationResolver.Object);
        _controller.AttachHttpContext(_sessionToken);
    }

    [Fact]
    public async Task CreateIdea_ForwardsCancellationTokenToIdeaManager()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;
        _moderationResolver.Setup(m => m.ResolveStatusAsync(It.IsAny<string>(), It.IsAny<string>(), ProjectId, ct))
            .ReturnsAsync(ModerationStatus.Approved);
        _ideaManager.Setup(i => i.AddIdeaAsync(CategoryId, ProjectId, "desc", ModerationStatus.Approved, _participant, ct))
            .ReturnsAsync(new Idea { IdeaId = 42 })
            .Verifiable(Times.Exactly(1));
        _ideaManager.Setup(i => i.GetIdeaCountByParticipantAsync(_participant.ParticipantId)).ReturnsAsync(1);
        _projectManager.Setup(p => p.GetProjectByIdAsync(ProjectId)).ReturnsAsync(new Project { ProjectId = ProjectId });

        // Act
        await _controller.CreateIdea(ProjectId, CategoryId, new CreateIdeaDto { Description = "desc" }, ct);

        // Assert
        _ideaManager.VerifyAll();
    }
}
