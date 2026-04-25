using SynapseEnqueteManagement.API.DTOs.Ideation;

namespace SynapseEnqueteManagement.API.Tests;

public class IdeasControllerTests
{
    private readonly Mock<IIdeaManager> _ideaManager = new();
    private readonly Mock<ICommentManager> _commentManager = new();
    private readonly Mock<IParticipantManager> _participantManager = new();
    private readonly Mock<IModerationResolver> _moderationResolver = new();
    private readonly Mock<IProjectManager> _projectManager = new();
    private readonly Mock<IEmbeddingSortStrategyFactory> _embeddingSortFactory = new();
    private readonly IdeasController _controller;

    private const int ProjectId = 1;
    private const int CategoryId = 2;
    private readonly Guid _sessionToken = Guid.NewGuid();
    private readonly Participant _participant;

    public IdeasControllerTests()
    {
        _participant = new Participant { ParticipantId = 5, Project = new Project { ProjectId = ProjectId } };
        _participantManager.Setup(p => p.GetParticipantBySessionTokenForUpdateAsync(_sessionToken))
            .ReturnsAsync(_participant);

        _controller = new IdeasController(
            _ideaManager.Object,
            _commentManager.Object,
            _participantManager.Object,
            _moderationResolver.Object,
            _projectManager.Object,
            _embeddingSortFactory.Object);
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
            .ReturnsAsync(new AddIdeaResult(42, false))
            .Verifiable(Times.Exactly(1));

        // Act
        await _controller.CreateIdea(ProjectId, CategoryId, new CreateIdeaDto { Description = "desc" }, ct);

        // Assert
        _ideaManager.VerifyAll();
    }
}
