using Microsoft.Extensions.Logging.Abstractions;
using SynapseEnqueteManagement.API.Services;

namespace SynapseEnqueteManagement.API.Tests.Unit;

public class QuestionMediaCleanupServiceTests
{
    private readonly Mock<IStorageService> _storage = new(MockBehavior.Strict);
    private readonly QuestionMediaCleanupService _service;

    public QuestionMediaCleanupServiceTests()
    {
        _service = new QuestionMediaCleanupService(_storage.Object, NullLogger<QuestionMediaCleanupService>.Instance);
    }

    [Fact]
    public async Task DeleteIfOrphanedAsync_UrlChanged_DeletesOldFile()
    {
        _storage.Setup(s => s.DeleteAsync("old.png")).Returns(Task.CompletedTask).Verifiable(Times.Once);

        await _service.DeleteIfOrphanedAsync("old.png", "new.png", isEmbed: false);

        _storage.VerifyAll();
    }

    [Fact]
    public async Task DeleteIfOrphanedAsync_UrlUnchanged_DoesNothing()
    {
        await _service.DeleteIfOrphanedAsync("same.png", "same.png", isEmbed: false);

        _storage.Verify(s => s.DeleteAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DeleteIfOrphanedAsync_OldUrlEmpty_DoesNothing()
    {
        await _service.DeleteIfOrphanedAsync(string.Empty, "new.png", isEmbed: false);

        _storage.Verify(s => s.DeleteAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DeleteIfOrphanedAsync_Embed_DoesNotTouchStorage()
    {
        // Embeds are external URLs (e.g. YouTube), not files we own.
        await _service.DeleteIfOrphanedAsync("old.png", "new.png", isEmbed: true);

        _storage.Verify(s => s.DeleteAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DeleteIfOrphanedAsync_StorageThrows_Swallowed()
    {
        _storage.Setup(s => s.DeleteAsync("old.png")).ThrowsAsync(new InvalidOperationException("boom"));

        // Must not propagate — orphan cleanup is best-effort.
        await _service.DeleteIfOrphanedAsync("old.png", "new.png", isEmbed: false);

        _storage.VerifyAll();
    }

    [Fact]
    public async Task DeleteOnRemovalAsync_NonEmptyNonEmbed_DeletesFile()
    {
        _storage.Setup(s => s.DeleteAsync("gone.png")).Returns(Task.CompletedTask).Verifiable(Times.Once);

        await _service.DeleteOnRemovalAsync("gone.png", isEmbed: false);

        _storage.VerifyAll();
    }

    [Fact]
    public async Task DeleteOnRemovalAsync_Embed_DoesNothing()
    {
        await _service.DeleteOnRemovalAsync("gone.png", isEmbed: true);

        _storage.Verify(s => s.DeleteAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DeleteOnRemovalAsync_Empty_DoesNothing()
    {
        await _service.DeleteOnRemovalAsync(null, isEmbed: false);

        _storage.Verify(s => s.DeleteAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DeleteOnRemovalAsync_StorageThrows_Swallowed()
    {
        _storage.Setup(s => s.DeleteAsync("gone.png")).ThrowsAsync(new InvalidOperationException("boom"));

        await _service.DeleteOnRemovalAsync("gone.png", isEmbed: false);

        _storage.VerifyAll();
    }
}
