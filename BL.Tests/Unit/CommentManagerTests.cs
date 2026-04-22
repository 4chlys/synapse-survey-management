using System.Linq.Expressions;
using Microsoft.Extensions.Logging.Abstractions;
using SynapseEnqueteManagement.BL.Sorting;

namespace SynapseEnqueteManagement.BL.Tests.Unit;

public class CommentManagerTests
{
    private readonly Mock<IRepository> _repository = new(MockBehavior.Strict);
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
        _manager = new CommentManager(_repository.Object, _uow.Object, NullLogger<CommentManager>.Instance, _sortFactory.Object);
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
        _repository.Setup(r => r.ReadByIdAsync<Idea>(IdeaId, It.IsAny<Expression<Func<Idea, object>>[]>(), It.IsAny<string[]>(), It.IsAny<bool>()))
            .ReturnsAsync(IdeaInProject())
            .Verifiable(Times.Exactly(1));
        _repository.Setup(r => r.CreateAsync(It.IsAny<TextComment>(), It.IsAny<Expression<Func<TextComment, bool>>>()))
            .ReturnsAsync((TextComment c, Expression<Func<TextComment, bool>> _) => c)
            .Verifiable(Times.Exactly(1));
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .Verifiable(Times.Exactly(1));

        // Act
        var created = await _manager.AddTextCommentAsync(IdeaId, ProjectId, "hello", ModerationStatus.Approved, _participant);

        // Assert
        Assert.Equal("hello", created.Text);
        _repository.VerifyAll();
        _uow.VerifyAll();
    }

    [Fact]
    public async Task AddTextCommentAsync_IdeaNotInProject_ThrowsAndDoesNotSave()
    {
        // Arrange
        _repository.Setup(r => r.ReadByIdAsync<Idea>(IdeaId, It.IsAny<Expression<Func<Idea, object>>[]>(), It.IsAny<string[]>(), It.IsAny<bool>()))
            .ReturnsAsync((Idea)null)
            .Verifiable(Times.Exactly(1));

        // Act + Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _manager.AddTextCommentAsync(IdeaId, ProjectId, "hello", ModerationStatus.Approved, _participant));

        _repository.VerifyAll();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region AddIconCommentAsync

    [Fact]
    public async Task AddIconCommentAsync_HappyPath_CreatesAndSavesOnce()
    {
        // Arrange
        _repository.Setup(r => r.ReadByIdAsync<Idea>(IdeaId, It.IsAny<Expression<Func<Idea, object>>[]>(), It.IsAny<string[]>(), It.IsAny<bool>()))
            .ReturnsAsync(IdeaInProject());
        _repository.Setup(r => r.ReadByIdAsync<Emoji>(EmojiId, It.IsAny<Expression<Func<Emoji, object>>[]>(), It.IsAny<string[]>(), It.IsAny<bool>()))
            .ReturnsAsync(new Emoji { EmojiId = EmojiId, Symbol = "👍", Name = "thumbsup" });
        _repository.Setup(r => r.CreateAsync(It.IsAny<IconComment>(), It.IsAny<Expression<Func<IconComment, bool>>>()))
            .ReturnsAsync((IconComment ic, Expression<Func<IconComment, bool>> _) => ic)
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
        _repository.Verify(r => r.CreateAsync(It.IsAny<IconComment>(), It.IsAny<Expression<Func<IconComment, bool>>>()), Times.Once);
    }

    [Fact]
    public async Task AddIconCommentAsync_DuplicateDetected_ReturnsNullWithoutSaving()
    {
        // Arrange
        _repository.Setup(r => r.ReadByIdAsync<Idea>(IdeaId, It.IsAny<Expression<Func<Idea, object>>[]>(), It.IsAny<string[]>(), It.IsAny<bool>()))
            .ReturnsAsync(IdeaInProject());
        _repository.Setup(r => r.ReadByIdAsync<Emoji>(EmojiId, It.IsAny<Expression<Func<Emoji, object>>[]>(), It.IsAny<string[]>(), It.IsAny<bool>()))
            .ReturnsAsync(new Emoji { EmojiId = EmojiId, Symbol = "👍", Name = "thumbsup" });
        _repository.Setup(r => r.CreateAsync(It.IsAny<IconComment>(), It.IsAny<Expression<Func<IconComment, bool>>>()))
            .ReturnsAsync((IconComment)null)
            .Verifiable(Times.Exactly(1));

        // Act
        var created = await _manager.AddIconCommentAsync(IdeaId, ProjectId, EmojiId, _participant);

        // Assert
        Assert.Null(created);
        _repository.Verify(r => r.CreateAsync(It.IsAny<IconComment>(), It.IsAny<Expression<Func<IconComment, bool>>>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddIconCommentAsync_EmojiNotEnabled_ThrowsWithoutSaving()
    {
        // Arrange — idea's project has no matching emoji
        var idea = IdeaInProject();
        idea.Category.Project.Emojis.Clear();
        _repository.Setup(r => r.ReadByIdAsync<Idea>(IdeaId, It.IsAny<Expression<Func<Idea, object>>[]>(), It.IsAny<string[]>(), It.IsAny<bool>()))
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
        _repository.Setup(r => r.DeleteAsync(It.IsAny<Expression<Func<IconComment, bool>>>()))
            .ReturnsAsync(true)
            .Verifiable(Times.Exactly(1));
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .Verifiable(Times.Exactly(1));

        // Act
        var removed = await _manager.RemoveIconCommentAsync(IdeaId, ProjectId, EmojiId, _participant);

        // Assert
        Assert.True(removed);
        _repository.VerifyAll();
        _uow.VerifyAll();
    }

    [Fact]
    public async Task RemoveIconCommentAsync_NotFound_ReturnsFalseWithoutSaving()
    {
        // Arrange
        _repository.Setup(r => r.DeleteAsync(It.IsAny<Expression<Func<IconComment, bool>>>()))
            .ReturnsAsync(false)
            .Verifiable(Times.Exactly(1));

        // Act
        var removed = await _manager.RemoveIconCommentAsync(IdeaId, ProjectId, EmojiId, _participant);

        // Assert
        Assert.False(removed);
        _repository.VerifyAll();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region AddCommentReactionAsync

    [Fact]
    public async Task AddCommentReactionAsync_HappyPath_CreatesAndSavesOnce()
    {
        // Arrange
        _repository.Setup(r => r.ReadByIdAsync<TextComment>(CommentId, It.IsAny<Expression<Func<TextComment, object>>[]>(), It.IsAny<string[]>(), It.IsAny<bool>()))
            .ReturnsAsync(TextCommentInProject());
        _repository.Setup(r => r.ReadByIdAsync<Emoji>(EmojiId, It.IsAny<Expression<Func<Emoji, object>>[]>(), It.IsAny<string[]>(), It.IsAny<bool>()))
            .ReturnsAsync(new Emoji { EmojiId = EmojiId, Symbol = "👍", Name = "thumbsup" });
        _repository.Setup(r => r.CreateAsync(It.IsAny<IconComment>(), It.IsAny<Expression<Func<IconComment, bool>>>()))
            .ReturnsAsync((IconComment ic, Expression<Func<IconComment, bool>> _) => ic)
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
        _repository.Setup(r => r.ReadByIdAsync<TextComment>(CommentId, It.IsAny<Expression<Func<TextComment, object>>[]>(), It.IsAny<string[]>(), It.IsAny<bool>()))
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
        _repository.Setup(r => r.ReadByIdAsync<TextComment>(CommentId, It.IsAny<Expression<Func<TextComment, object>>[]>(), It.IsAny<string[]>(), It.IsAny<bool>()))
            .ReturnsAsync(TextCommentInProject());
        _repository.Setup(r => r.ReadByIdAsync<Emoji>(EmojiId, It.IsAny<Expression<Func<Emoji, object>>[]>(), It.IsAny<string[]>(), It.IsAny<bool>()))
            .ReturnsAsync(new Emoji { EmojiId = EmojiId, Symbol = "👍", Name = "thumbsup" });
        _repository.Setup(r => r.CreateAsync(It.IsAny<IconComment>(), It.IsAny<Expression<Func<IconComment, bool>>>()))
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
        _repository.Setup(r => r.DeleteAsync(It.IsAny<Expression<Func<IconComment, bool>>>()))
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
        _repository.Setup(r => r.DeleteAsync(It.IsAny<Expression<Func<IconComment, bool>>>()))
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
        _repository.Setup(r => r.ReadByIdAsync<Idea>(IdeaId, It.IsAny<Expression<Func<Idea, object>>[]>(), It.IsAny<string[]>(), It.IsAny<bool>()))
            .ReturnsAsync(IdeaInProject());
        _repository.Setup(r => r.CreateAsync(It.IsAny<TextComment>(), It.IsAny<Expression<Func<TextComment, bool>>>()))
            .ReturnsAsync((TextComment c, Expression<Func<TextComment, bool>> _) => c);
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
