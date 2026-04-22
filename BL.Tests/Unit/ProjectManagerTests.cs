namespace SynapseEnqueteManagement.BL.Tests.Unit;

public class ProjectManagerTests
{
    private readonly Mock<IProjectRepository> _projectRepository = new(MockBehavior.Strict);
    private readonly Mock<IEmojiRepository> _emojiRepository = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _uow = new(MockBehavior.Strict);
    private readonly ProjectManager _manager;

    private const int ProjectId = 1;
    private const int EmojiId = 99;

    public ProjectManagerTests()
    {
        _manager = new ProjectManager(_projectRepository.Object, _emojiRepository.Object, _uow.Object);
    }

    #region EnableEmojiForProjectAsync

    [Fact]
    public async Task EnableEmojiForProjectAsync_HappyPath_AttachesEmojiAndSavesOnce()
    {
        // Arrange
        var project = new Project { ProjectId = ProjectId, Emojis = new List<Emoji>() };
        var emoji = new Emoji { EmojiId = EmojiId, Symbol = "👍", Name = "thumbsup" };
        _projectRepository.Setup(r => r.ReadByIdWithEmojisForUpdateAsync(ProjectId))
            .ReturnsAsync(project);
        _emojiRepository.Setup(r => r.ReadByIdForUpdateAsync(EmojiId))
            .ReturnsAsync(emoji);
        _projectRepository.Setup(r => r.Update(project)).Verifiable(Times.Exactly(1));
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .Verifiable(Times.Exactly(1));

        // Act
        var result = await _manager.EnableEmojiForProjectAsync(ProjectId, EmojiId);

        // Assert
        Assert.True(result);
        Assert.Contains(emoji, project.Emojis);
        _projectRepository.VerifyAll();
        _uow.VerifyAll();
    }

    [Fact]
    public async Task EnableEmojiForProjectAsync_ForwardsCancellationTokenToUnitOfWork()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;
        var project = new Project { ProjectId = ProjectId, Emojis = new List<Emoji>() };
        _projectRepository.Setup(r => r.ReadByIdWithEmojisForUpdateAsync(ProjectId))
            .ReturnsAsync(project);
        _emojiRepository.Setup(r => r.ReadByIdForUpdateAsync(EmojiId))
            .ReturnsAsync(new Emoji { EmojiId = EmojiId, Symbol = "👍", Name = "thumbsup" });
        _projectRepository.Setup(r => r.Update(project));
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
        _projectRepository.Setup(r => r.ReadByIdWithEmojisForUpdateAsync(ProjectId))
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
        _projectRepository.Setup(r => r.ReadByIdWithEmojisForUpdateAsync(ProjectId))
            .ReturnsAsync(new Project { ProjectId = ProjectId, Emojis = new List<Emoji>() });
        _emojiRepository.Setup(r => r.ReadByIdForUpdateAsync(EmojiId))
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
        _projectRepository.Setup(r => r.ReadByIdWithEmojisForUpdateAsync(ProjectId))
            .ReturnsAsync(project);
        _emojiRepository.Setup(r => r.ReadByIdForUpdateAsync(EmojiId))
            .ReturnsAsync(emoji);

        // Act
        var result = await _manager.EnableEmojiForProjectAsync(ProjectId, EmojiId);

        // Assert
        Assert.True(result);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _projectRepository.Verify(r => r.Update(It.IsAny<Project>()), Times.Never);
    }

    #endregion

    #region DisableEmojiForProjectAsync

    [Fact]
    public async Task DisableEmojiForProjectAsync_HappyPath_RemovesEmojiAndSavesOnce()
    {
        // Arrange
        var emoji = new Emoji { EmojiId = EmojiId, Symbol = "👍", Name = "thumbsup" };
        var project = new Project { ProjectId = ProjectId, Emojis = new List<Emoji> { emoji } };
        _projectRepository.Setup(r => r.ReadByIdWithEmojisForUpdateAsync(ProjectId))
            .ReturnsAsync(project);
        _projectRepository.Setup(r => r.Update(project)).Verifiable(Times.Exactly(1));
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .Verifiable(Times.Exactly(1));

        // Act
        var result = await _manager.DisableEmojiForProjectAsync(ProjectId, EmojiId);

        // Assert
        Assert.True(result);
        Assert.DoesNotContain(emoji, project.Emojis);
        _projectRepository.VerifyAll();
        _uow.VerifyAll();
    }

    [Fact]
    public async Task DisableEmojiForProjectAsync_ProjectNotFound_ReturnsFalseWithoutSaving()
    {
        // Arrange
        _projectRepository.Setup(r => r.ReadByIdWithEmojisForUpdateAsync(ProjectId))
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
        _projectRepository.Setup(r => r.ReadByIdWithEmojisForUpdateAsync(ProjectId))
            .ReturnsAsync(new Project { ProjectId = ProjectId, Emojis = new List<Emoji>() });

        // Act
        var result = await _manager.DisableEmojiForProjectAsync(ProjectId, EmojiId);

        // Assert
        Assert.False(result);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _projectRepository.Verify(r => r.Update(It.IsAny<Project>()), Times.Never);
    }

    #endregion
}
