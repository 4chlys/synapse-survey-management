namespace SynapseEnqueteManagement.API.Tests;

public class EmojisControllerTests
{
    private readonly Mock<IProjectManager> _projectManager = new();
    private readonly Mock<IEmojiManager> _emojiManager = new();
    private readonly EmojisController _controller;

    public EmojisControllerTests()
    {
        _controller = new EmojisController(_projectManager.Object, _emojiManager.Object);
    }

    [Fact]
    public async Task EnableEmoji_ForwardsCancellationTokenToProjectManager()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;
        _projectManager.Setup(p => p.EnableEmojiForProjectAsync(1, 2, ct))
            .ReturnsAsync(true)
            .Verifiable(Times.Exactly(1));

        // Act
        await _controller.EnableEmoji(1, 2, ct);

        // Assert
        _projectManager.VerifyAll();
    }

    [Fact]
    public async Task DisableEmoji_ForwardsCancellationTokenToProjectManager()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;
        _projectManager.Setup(p => p.DisableEmojiForProjectAsync(1, 2, ct))
            .ReturnsAsync(true)
            .Verifiable(Times.Exactly(1));

        // Act
        await _controller.DisableEmoji(1, 2, ct);

        // Assert
        _projectManager.VerifyAll();
    }
}
