using System.Linq.Expressions;

namespace SynapseEnqueteManagement.BL.Tests.Unit;

public class ProjectManagerTests
{
    private readonly Mock<IRepository> _repository = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _uow = new(MockBehavior.Strict);
    private readonly ProjectManager _manager;

    private const int ProjectId = 1;
    private const int EmojiId = 99;

    public ProjectManagerTests()
    {
        _manager = new ProjectManager(_repository.Object, _uow.Object);
    }

    #region EnableEmojiForProjectAsync

    [Fact]
    public async Task EnableEmojiForProjectAsync_HappyPath_AttachesEmojiAndSavesOnce()
    {
        // Arrange
        var project = new Project { ProjectId = ProjectId, Emojis = new List<Emoji>() };
        var emoji = new Emoji { EmojiId = EmojiId, Symbol = "👍", Name = "thumbsup" };
        _repository.Setup(r => r.ReadByIdAsync<Project>(ProjectId, It.IsAny<Expression<Func<Project, object>>[]>(), It.IsAny<string[]>(), It.IsAny<bool>()))
            .ReturnsAsync(project);
        _repository.Setup(r => r.ReadByIdAsync<Emoji>(EmojiId, It.IsAny<Expression<Func<Emoji, object>>[]>(), It.IsAny<string[]>(), It.IsAny<bool>()))
            .ReturnsAsync(emoji);
        _repository.Setup(r => r.Update(project)).Verifiable(Times.Exactly(1));
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .Verifiable(Times.Exactly(1));

        // Act
        var result = await _manager.EnableEmojiForProjectAsync(ProjectId, EmojiId);

        // Assert
        Assert.True(result);
        Assert.Contains(emoji, project.Emojis);
        _repository.VerifyAll();
        _uow.VerifyAll();
    }

    [Fact]
    public async Task EnableEmojiForProjectAsync_ForwardsCancellationTokenToUnitOfWork()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;
        var project = new Project { ProjectId = ProjectId, Emojis = new List<Emoji>() };
        _repository.Setup(r => r.ReadByIdAsync<Project>(ProjectId, It.IsAny<Expression<Func<Project, object>>[]>(), It.IsAny<string[]>(), It.IsAny<bool>()))
            .ReturnsAsync(project);
        _repository.Setup(r => r.ReadByIdAsync<Emoji>(EmojiId, It.IsAny<Expression<Func<Emoji, object>>[]>(), It.IsAny<string[]>(), It.IsAny<bool>()))
            .ReturnsAsync(new Emoji { EmojiId = EmojiId, Symbol = "👍", Name = "thumbsup" });
        _repository.Setup(r => r.Update(project));
        _uow.Setup(u => u.SaveChangesAsync(ct))
            .ReturnsAsync(1)
            .Verifiable(Times.Exactly(1));

        // Act
        await _manager.EnableEmojiForProjectAsync(ProjectId, EmojiId, ct);

        // Assert
        _uow.VerifyAll();
    }

    [Fact]
    public async Task EnableEmojiForProjectAsync_ProjectNotFound_ReturnsFalseWithoutSaving()
    {
        // Arrange
        _repository.Setup(r => r.ReadByIdAsync<Project>(ProjectId, It.IsAny<Expression<Func<Project, object>>[]>(), It.IsAny<string[]>(), It.IsAny<bool>()))
            .ReturnsAsync((Project)null);

        // Act
        var result = await _manager.EnableEmojiForProjectAsync(ProjectId, EmojiId);

        // Assert
        Assert.False(result);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EnableEmojiForProjectAsync_EmojiNotFound_ReturnsFalseWithoutSaving()
    {
        // Arrange
        _repository.Setup(r => r.ReadByIdAsync<Project>(ProjectId, It.IsAny<Expression<Func<Project, object>>[]>(), It.IsAny<string[]>(), It.IsAny<bool>()))
            .ReturnsAsync(new Project { ProjectId = ProjectId, Emojis = new List<Emoji>() });
        _repository.Setup(r => r.ReadByIdAsync<Emoji>(EmojiId, It.IsAny<Expression<Func<Emoji, object>>[]>(), It.IsAny<string[]>(), It.IsAny<bool>()))
            .ReturnsAsync((Emoji)null);

        // Act
        var result = await _manager.EnableEmojiForProjectAsync(ProjectId, EmojiId);

        // Assert
        Assert.False(result);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EnableEmojiForProjectAsync_AlreadyEnabled_ReturnsTrueWithoutSaving()
    {
        // Arrange
        var emoji = new Emoji { EmojiId = EmojiId, Symbol = "👍", Name = "thumbsup" };
        var project = new Project { ProjectId = ProjectId, Emojis = new List<Emoji> { emoji } };
        _repository.Setup(r => r.ReadByIdAsync<Project>(ProjectId, It.IsAny<Expression<Func<Project, object>>[]>(), It.IsAny<string[]>(), It.IsAny<bool>()))
            .ReturnsAsync(project);
        _repository.Setup(r => r.ReadByIdAsync<Emoji>(EmojiId, It.IsAny<Expression<Func<Emoji, object>>[]>(), It.IsAny<string[]>(), It.IsAny<bool>()))
            .ReturnsAsync(emoji);

        // Act
        var result = await _manager.EnableEmojiForProjectAsync(ProjectId, EmojiId);

        // Assert
        Assert.True(result);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(r => r.Update(It.IsAny<Project>()), Times.Never);
    }

    #endregion

    #region DisableEmojiForProjectAsync

    [Fact]
    public async Task DisableEmojiForProjectAsync_HappyPath_RemovesEmojiAndSavesOnce()
    {
        // Arrange
        var emoji = new Emoji { EmojiId = EmojiId, Symbol = "👍", Name = "thumbsup" };
        var project = new Project { ProjectId = ProjectId, Emojis = new List<Emoji> { emoji } };
        _repository.Setup(r => r.ReadByIdAsync<Project>(ProjectId, It.IsAny<Expression<Func<Project, object>>[]>(), It.IsAny<string[]>(), It.IsAny<bool>()))
            .ReturnsAsync(project);
        _repository.Setup(r => r.Update(project)).Verifiable(Times.Exactly(1));
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .Verifiable(Times.Exactly(1));

        // Act
        var result = await _manager.DisableEmojiForProjectAsync(ProjectId, EmojiId);

        // Assert
        Assert.True(result);
        Assert.DoesNotContain(emoji, project.Emojis);
        _repository.VerifyAll();
        _uow.VerifyAll();
    }

    [Fact]
    public async Task DisableEmojiForProjectAsync_ProjectNotFound_ReturnsFalseWithoutSaving()
    {
        // Arrange
        _repository.Setup(r => r.ReadByIdAsync<Project>(ProjectId, It.IsAny<Expression<Func<Project, object>>[]>(), It.IsAny<string[]>(), It.IsAny<bool>()))
            .ReturnsAsync((Project)null);

        // Act
        var result = await _manager.DisableEmojiForProjectAsync(ProjectId, EmojiId);

        // Assert
        Assert.False(result);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DisableEmojiForProjectAsync_EmojiNotEnabled_ReturnsFalseWithoutSaving()
    {
        // Arrange
        _repository.Setup(r => r.ReadByIdAsync<Project>(ProjectId, It.IsAny<Expression<Func<Project, object>>[]>(), It.IsAny<string[]>(), It.IsAny<bool>()))
            .ReturnsAsync(new Project { ProjectId = ProjectId, Emojis = new List<Emoji>() });

        // Act
        var result = await _manager.DisableEmojiForProjectAsync(ProjectId, EmojiId);

        // Assert
        Assert.False(result);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(r => r.Update(It.IsAny<Project>()), Times.Never);
    }

    #endregion
}
