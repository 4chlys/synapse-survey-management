using Microsoft.EntityFrameworkCore;

namespace SynapseEnqueteManagement.DAL.Tests.Integration;

public class RepositoryTests : IDisposable
{
    private readonly InMemoryDatabase _db = new();

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task CreateAsync_WithoutSaveChanges_DoesNotPersist()
    {
        // Arrange
        await using var arrangeContext = _db.NewContext();
        var language = TestEntities.NewLanguage();
        arrangeContext.Languages.Add(language);
        await arrangeContext.SaveChangesAsync();
        var repository = new Repository<Project>(arrangeContext);
        var project = TestEntities.NewProject(language);

        // Act
        await repository.CreateAsync(project);

        // Assert
        await using var verifyContext = _db.NewContext();
        Assert.Empty(verifyContext.Projects);
    }

    [Fact]
    public async Task Update_WithoutSaveChanges_DoesNotPersist()
    {
        // Arrange
        await using var seedContext = _db.NewContext();
        var language = TestEntities.NewLanguage();
        seedContext.Languages.Add(language);
        var project = TestEntities.NewProject(language, "Original");
        seedContext.Projects.Add(project);
        await seedContext.SaveChangesAsync();
        var projectId = project.ProjectId;

        await using var actContext = _db.NewContext();
        var tracked = await actContext.Projects
            .Include(p => p.Translations)
            .FirstOrDefaultAsync(p => p.ProjectId == projectId);
        if (tracked != null)
        {
            tracked.Translations.First().Name = "Modified";
            var repository = new Repository<Project>(actContext);

            // Act
            repository.Update(tracked);
        }

        // Assert
        await using var verifyContext = _db.NewContext();
        var stored = await verifyContext.Projects
            .Include(p => p.Translations)
            .FirstOrDefaultAsync(p => p.ProjectId == projectId);
        Assert.Equal("Original", stored?.Translations.First().Name);
    }

    [Fact]
    public async Task DeleteByIdAsync_PersistsImmediately()
    {
        // ExecuteDeleteAsync runs outside the change tracker, so the row is gone without a SaveChanges.
        // Callers must wrap this in IUnitOfWork.ExecuteInTransactionAsync to keep rollback semantics.
        await using var seedContext = _db.NewContext();
        var emoji = TestEntities.NewEmoji();
        seedContext.Emojis.Add(emoji);
        await seedContext.SaveChangesAsync();
        var emojiId = emoji.EmojiId;

        await using var actContext = _db.NewContext();
        var repository = new Repository<Emoji>(actContext);

        var removed = await repository.DeleteByIdAsync(emojiId);

        Assert.True(removed);
        await using var verifyContext = _db.NewContext();
        Assert.Empty(verifyContext.Emojis);
    }

    [Fact]
    public async Task DeleteByIdAsync_ReturnsFalse_WhenEntityNotFound()
    {
        // Arrange
        await using var context = _db.NewContext();
        var repository = new Repository<Emoji>(context);

        // Act
        var removed = await repository.DeleteByIdAsync(9999);

        // Assert
        Assert.False(removed);
    }
}
