using SynapseEnqueteManagement.API.Ai;
using SynapseEnqueteManagement.API.DTOs.Ideation;
using SynapseEnqueteManagement.BL.Ai;

namespace SynapseEnqueteManagement.API.Tests;

public class CommentsControllerTests
{
    private readonly Mock<ICommentManager> _commentManager = new();
    private readonly Mock<IParticipantManager> _participantManager = new();
    private readonly Mock<IModerationResolver> _moderationResolver = new();
    private readonly Mock<IProjectManager> _projectManager = new();
    private readonly Mock<IPlatformAiConfigService> _platformAiConfig = new();
    private readonly CommentsController _controller;

    private const int ProjectId = 1;
    private const int IdeaId = 10;
    private const int CommentId = 20;
    private const int EmojiId = 30;
    private readonly Guid _sessionToken = Guid.NewGuid();
    private readonly Participant _participant;

    public CommentsControllerTests()
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
        _controller = new CommentsController(_commentManager.Object, _participantManager.Object, _moderationResolver.Object, _projectManager.Object, _platformAiConfig.Object);
        _controller.AttachHttpContext(_sessionToken);
    }

    [Fact]
    public async Task AddComment_ForwardsCancellationTokenToCommentManager()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;
        _moderationResolver.Setup(m => m.ResolveStatusAsync(It.IsAny<string>(), It.IsAny<string>(), ProjectId, It.IsAny<ResolvedPlatformAiConfig>(), ct, It.IsAny<string>()))
            .ReturnsAsync(ModerationStatus.Approved);
        _commentManager.Setup(c => c.AddTextCommentAsync(IdeaId, ProjectId, "hi", ModerationStatus.Approved, _participant, null, ct))
            .ReturnsAsync(new TextComment { CommentId = 1, Text = "hi" })
            .Verifiable(Times.Exactly(1));

        // Act
        await _controller.AddComment(ProjectId, IdeaId, new CreateCommentDto { Text = "hi" }, ct);

        // Assert
        _commentManager.VerifyAll();
    }

    [Fact]
    public async Task AddIconComment_ForwardsCancellationTokenToCommentManager()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;
        _commentManager.Setup(c => c.AddIconCommentAsync(IdeaId, ProjectId, EmojiId, _participant, ct))
            .ReturnsAsync(new IconComment { Emoji = new Emoji { EmojiId = EmojiId } })
            .Verifiable(Times.Exactly(1));

        // Act
        await _controller.AddIconComment(ProjectId, IdeaId, new AddIconCommentDto { EmojiId = EmojiId }, ct);

        // Assert
        _commentManager.VerifyAll();
    }

    [Fact]
    public async Task RemoveIconComment_ForwardsCancellationTokenToCommentManager()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;
        _commentManager.Setup(c => c.RemoveIconCommentAsync(IdeaId, ProjectId, EmojiId, _participant, ct))
            .ReturnsAsync(true)
            .Verifiable(Times.Exactly(1));

        // Act
        await _controller.RemoveIconComment(ProjectId, IdeaId, EmojiId, ct);

        // Assert
        _commentManager.VerifyAll();
    }

    [Fact]
    public async Task AddCommentEmoji_ForwardsCancellationTokenToCommentManager()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;
        _commentManager.Setup(c => c.AddCommentReactionAsync(CommentId, ProjectId, EmojiId, _participant, ct))
            .ReturnsAsync(new IconComment { Emoji = new Emoji { EmojiId = EmojiId } })
            .Verifiable(Times.Exactly(1));

        // Act
        await _controller.AddCommentEmoji(ProjectId, IdeaId, CommentId, new AddIconCommentDto { EmojiId = EmojiId }, ct);

        // Assert
        _commentManager.VerifyAll();
    }

    [Fact]
    public async Task RemoveCommentEmoji_ForwardsCancellationTokenToCommentManager()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;
        _commentManager.Setup(c => c.RemoveCommentReactionAsync(CommentId, ProjectId, EmojiId, _participant, ct))
            .ReturnsAsync(true)
            .Verifiable(Times.Exactly(1));

        // Act
        await _controller.RemoveCommentEmoji(ProjectId, IdeaId, CommentId, EmojiId, ct);

        // Assert
        _commentManager.VerifyAll();
    }
}
