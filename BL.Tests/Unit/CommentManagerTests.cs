using Microsoft.Extensions.Logging.Abstractions;
using SynapseEnqueteManagement.BL.Sorting;

namespace SynapseEnqueteManagement.BL.Tests.Unit;

public class CommentManagerTests
{
    private readonly Mock<ICommentRepository> _commentRepository = new(MockBehavior.Strict);
    private readonly Mock<IIdeaRepository> _ideaRepository = new(MockBehavior.Strict);
    private readonly Mock<IProjectRepository> _projectRepository = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _uow = new(MockBehavior.Strict);
    private readonly Mock<ISortStrategyFactory<TextComment>> _sortFactory = new();
    private readonly CommentManager _manager;

    private readonly Participant _participant = new() { ParticipantId = 7 };
    private const int IdeaId = 10;
    private const int ProjectId = 100;
    private const int EmojiId = 50;
    private const int CommentId = 200;

    public CommentManagerTests()
    {
        _manager = new CommentManager(_commentRepository.Object, _ideaRepository.Object, _projectRepository.Object, _uow.Object, NullLogger<CommentManager>.Instance, _sortFactory.Object);
    }

    private Idea IdeaInProject() => new()
    {
        IdeaId = IdeaId,
        Category = new Category
        {
            Project = new Project
            {
                ProjectId = ProjectId,
                Emojis = new List<Emoji> { new() { EmojiId = EmojiId, Symbol = "👍", Name = "thumbsup" } }
            }
        }
    };

    private TextComment TextCommentInProject() => new()
    {
        CommentId = CommentId,
        Idea = IdeaInProject()
    };

    #region AddTextCommentAsync

    [Fact]
    public async Task AddTextCommentAsync_HappyPath_CreatesCommentAndSavesOnce()
    {
        // Arrange
        _ideaRepository.Setup(r => r.ReadByIdWithProjectForUpdateAsync(IdeaId))
            .ReturnsAsync(IdeaInProject())
            .Verifiable(Times.Exactly(1));
        _commentRepository.Setup(r => r.CreateAsync(It.IsAny<TextComment>()))
            .ReturnsAsync((TextComment c) => c)
            .Verifiable(Times.Exactly(1));
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .Verifiable(Times.Exactly(1));

        // Act
        var created = await _manager.AddTextCommentAsync(IdeaId, ProjectId, "hello", ModerationStatus.Approved, _participant);

        // Assert
        Assert.Equal("hello", created.Text);
        _ideaRepository.VerifyAll();
        _commentRepository.VerifyAll();
        _uow.VerifyAll();
    }

    [Fact]
    public async Task AddTextCommentAsync_IdeaNotInProject_ThrowsAndDoesNotSave()
    {
        // Arrange
        _ideaRepository.Setup(r => r.ReadByIdWithProjectForUpdateAsync(IdeaId))
            .ReturnsAsync((Idea)null)
            .Verifiable(Times.Exactly(1));

        // Act + Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _manager.AddTextCommentAsync(IdeaId, ProjectId, "hello", ModerationStatus.Approved, _participant));

        _ideaRepository.VerifyAll();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region AddIconCommentAsync

    [Fact]
    public async Task AddIconCommentAsync_HappyPath_CreatesAndSavesOnce()
    {
        // Arrange
        _ideaRepository.Setup(r => r.ReadByIdWithProjectAndEmojisForUpdateAsync(IdeaId))
            .ReturnsAsync(IdeaInProject());
        _commentRepository.Setup(r => r.CreateIconCommentOnIdeaIfNotDuplicateAsync(It.IsAny<IconComment>()))
            .ReturnsAsync((IconComment ic) => ic)
            .Verifiable(Times.Exactly(1));
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .Verifiable(Times.Exactly(1));

        // Act
        var created = await _manager.AddIconCommentAsync(IdeaId, ProjectId, EmojiId, _participant);

        // Assert
        Assert.NotNull(created);
        Assert.Equal(EmojiId, created.EmojiId);
        _uow.VerifyAll();
        _commentRepository.VerifyAll();
    }

    [Fact]
    public async Task AddIconCommentAsync_DuplicateDetected_ReturnsNullWithoutSaving()
    {
        // Arrange
        _ideaRepository.Setup(r => r.ReadByIdWithProjectAndEmojisForUpdateAsync(IdeaId))
            .ReturnsAsync(IdeaInProject());
        _commentRepository.Setup(r => r.CreateIconCommentOnIdeaIfNotDuplicateAsync(It.IsAny<IconComment>()))
            .ReturnsAsync((IconComment)null)
            .Verifiable(Times.Exactly(1));

        // Act
        var created = await _manager.AddIconCommentAsync(IdeaId, ProjectId, EmojiId, _participant);

        // Assert
        Assert.Null(created);
        _commentRepository.VerifyAll();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddIconCommentAsync_EmojiNotEnabled_ThrowsWithoutSaving()
    {
        // Arrange — idea's project has no matching emoji
        var idea = IdeaInProject();
        idea.Category.Project.Emojis.Clear();
        _ideaRepository.Setup(r => r.ReadByIdWithProjectAndEmojisForUpdateAsync(IdeaId))
            .ReturnsAsync(idea);

        // Act + Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _manager.AddIconCommentAsync(IdeaId, ProjectId, EmojiId, _participant));

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region RemoveIconCommentAsync

    [Fact]
    public async Task RemoveIconCommentAsync_HappyPath_DeletesAndSavesOnce()
    {
        // Arrange
        _commentRepository.Setup(r => r.DeleteIconCommentOnIdeaAsync(IdeaId, EmojiId, _participant.ParticipantId))
            .ReturnsAsync(true)
            .Verifiable(Times.Exactly(1));
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .Verifiable(Times.Exactly(1));

        // Act
        var removed = await _manager.RemoveIconCommentAsync(IdeaId, ProjectId, EmojiId, _participant);

        // Assert
        Assert.True(removed);
        _commentRepository.VerifyAll();
        _uow.VerifyAll();
    }

    [Fact]
    public async Task RemoveIconCommentAsync_NotFound_ReturnsFalseWithoutSaving()
    {
        // Arrange
        _commentRepository.Setup(r => r.DeleteIconCommentOnIdeaAsync(IdeaId, EmojiId, _participant.ParticipantId))
            .ReturnsAsync(false)
            .Verifiable(Times.Exactly(1));

        // Act
        var removed = await _manager.RemoveIconCommentAsync(IdeaId, ProjectId, EmojiId, _participant);

        // Assert
        Assert.False(removed);
        _commentRepository.VerifyAll();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region AddCommentReactionAsync

    [Fact]
    public async Task AddCommentReactionAsync_HappyPath_CreatesAndSavesOnce()
    {
        // Arrange
        _commentRepository.Setup(r => r.ReadTextCommentByIdWithProjectAndEmojisForUpdateAsync(CommentId))
            .ReturnsAsync(TextCommentInProject());
        _commentRepository.Setup(r => r.CreateIconCommentOnTextCommentIfNotDuplicateAsync(It.IsAny<IconComment>()))
            .ReturnsAsync((IconComment ic) => ic)
            .Verifiable(Times.Exactly(1));
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .Verifiable(Times.Exactly(1));

        // Act
        var reaction = await _manager.AddCommentReactionAsync(CommentId, ProjectId, EmojiId, _participant);

        // Assert
        Assert.NotNull(reaction);
        _uow.VerifyAll();
    }

    [Fact]
    public async Task AddCommentReactionAsync_CommentNotInProject_ThrowsWithoutSaving()
    {
        // Arrange
        _commentRepository.Setup(r => r.ReadTextCommentByIdWithProjectAndEmojisForUpdateAsync(CommentId))
            .ReturnsAsync((TextComment)null);

        // Act + Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _manager.AddCommentReactionAsync(CommentId, ProjectId, EmojiId, _participant));

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddCommentReactionAsync_DuplicateDetected_ReturnsNullWithoutSaving()
    {
        // Arrange
        _commentRepository.Setup(r => r.ReadTextCommentByIdWithProjectAndEmojisForUpdateAsync(CommentId))
            .ReturnsAsync(TextCommentInProject());
        _commentRepository.Setup(r => r.CreateIconCommentOnTextCommentIfNotDuplicateAsync(It.IsAny<IconComment>()))
            .ReturnsAsync((IconComment)null);

        // Act
        var reaction = await _manager.AddCommentReactionAsync(CommentId, ProjectId, EmojiId, _participant);

        // Assert
        Assert.Null(reaction);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region RemoveCommentReactionAsync

    [Fact]
    public async Task RemoveCommentReactionAsync_HappyPath_DeletesAndSavesOnce()
    {
        // Arrange
        _commentRepository.Setup(r => r.DeleteIconCommentOnTextCommentAsync(CommentId, EmojiId, _participant.ParticipantId))
            .ReturnsAsync(true);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .Verifiable(Times.Exactly(1));

        // Act
        var removed = await _manager.RemoveCommentReactionAsync(CommentId, ProjectId, EmojiId, _participant);

        // Assert
        Assert.True(removed);
        _uow.VerifyAll();
    }

    [Fact]
    public async Task RemoveCommentReactionAsync_NotFound_ReturnsFalseWithoutSaving()
    {
        // Arrange
        _commentRepository.Setup(r => r.DeleteIconCommentOnTextCommentAsync(CommentId, EmojiId, _participant.ParticipantId))
            .ReturnsAsync(false);

        // Act
        var removed = await _manager.RemoveCommentReactionAsync(CommentId, ProjectId, EmojiId, _participant);

        // Assert
        Assert.False(removed);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Cancellation token forwarding

    [Fact]
    public async Task AddTextCommentAsync_ForwardsCancellationTokenToUnitOfWork()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;
        _ideaRepository.Setup(r => r.ReadByIdWithProjectForUpdateAsync(IdeaId))
            .ReturnsAsync(IdeaInProject());
        _commentRepository.Setup(r => r.CreateAsync(It.IsAny<TextComment>()))
            .ReturnsAsync((TextComment c) => c);
        _uow.Setup(u => u.SaveChangesAsync(ct))
            .ReturnsAsync(1)
            .Verifiable(Times.Exactly(1));

        // Act
        await _manager.AddTextCommentAsync(IdeaId, ProjectId, "hello", ModerationStatus.Approved, _participant, ct);

        // Assert
        _uow.VerifyAll();
    }

    #endregion
}
