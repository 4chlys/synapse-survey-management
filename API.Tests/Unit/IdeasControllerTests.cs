using Microsoft.Extensions.Logging.Abstractions;
using SynapseEnqueteManagement.API.Ai;
using SynapseEnqueteManagement.API.DTOs.Ideation;
using SynapseEnqueteManagement.API.Services;
using SynapseEnqueteManagement.BL.Ai;

namespace SynapseEnqueteManagement.API.Tests;

public class IdeasControllerTests
{
    private readonly Mock<IIdeaManager> _ideaManager = new();
    private readonly Mock<ICommentManager> _commentManager = new();
    private readonly Mock<IParticipantManager> _participantManager = new();
    private readonly Mock<IModerationResolver> _moderationResolver = new();
    private readonly Mock<IProjectManager> _projectManager = new();
    private readonly Mock<IStorageService> _storageService = new();
    private readonly Mock<IModerationTokenService> _moderationTokenService = new();
    private readonly Mock<IPlatformAiConfigService> _platformAiConfig = new();
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

        _platformAiConfig.Setup(s => s.ResolveAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResolvedPlatformAiConfig(
                Mock.Of<Microsoft.Extensions.AI.IChatClient>(),
                new SynapseEnqueteManagement.BL.AiModeration.ModerationConfig(),
                new SynapseEnqueteManagement.BL.AiIdeaSupport.AiIdeaSupportConfig(),
                new SynapseEnqueteManagement.BL.AiCommentSupport.AiCommentSupportConfig(),
                new SynapseEnqueteManagement.BL.AiQuestionSupport.AiQuestionSupportConfig()));
        _controller = new IdeasController(
            _ideaManager.Object,
            _commentManager.Object,
            _participantManager.Object,
            _moderationResolver.Object,
            _projectManager.Object,
            _storageService.Object,
            _moderationTokenService.Object,
            _platformAiConfig.Object,
            NullLogger<IdeasController>.Instance);
        _controller.AttachHttpContext(_sessionToken);
    }

    [Fact]
    public async Task GetIdeasByCategory_ForwardsShowAllLanguagesToManager()
    {
        // Arrange
        var expectedResult = new IdeaIndexResult
        {
            Items = new List<Idea>(),
            Page = 1,
            PageSize = 5,
            TotalCount = 0,
            ResolvedSort = "diverse",
            HasOwnIdeas = false
        };
        _ideaManager
            .Setup(m => m.GetIdeasIndexAsync(CategoryId, _participant.ParticipantId, 5, 1, null, null, true, default))
            .ReturnsAsync(expectedResult)
            .Verifiable(Times.Exactly(1));

        // Act
        await _controller.GetIdeasByCategory(ProjectId, CategoryId, showAllLanguages: true);

        // Assert
        _ideaManager.VerifyAll();
    }

    [Fact]
    public async Task CreateIdea_ForwardsCancellationTokenToIdeaManager()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;
        _moderationResolver.Setup(m => m.ResolveStatusAsync(It.IsAny<string>(), It.IsAny<string>(), ProjectId, It.IsAny<ResolvedPlatformAiConfig>(), ct, It.IsAny<string>()))
            .ReturnsAsync(ModerationStatus.Approved);
        _ideaManager.Setup(i => i.AddIdeaAsync(CategoryId, ProjectId, "desc", ModerationStatus.Approved, _participant, null, null, ct))
            .ReturnsAsync(new AddIdeaResult(42, false))
            .Verifiable(Times.Exactly(1));

        // Act
        await _controller.CreateIdea(ProjectId, CategoryId, new CreateIdeaDto { Description = "desc" }, ct);

        // Assert
        _ideaManager.VerifyAll();
    }
}
