using SynapseEnqueteManagement.BL.Domain.Project.Entities;

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
        var repository = new Repository(arrangeContext);
        var project = TestEntities.NewProject();

        // Act
        await repository.CreateAsync(project);

        // Assert
        await using var verifyContext = _db.NewContext();
        Assert.Empty(verifyContext.Projects);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateCheck_ReturnsNull_WhenMatchExists()
    {
        // Arrange
        await using var context = _db.NewContext();
        context.Emojis.Add(TestEntities.NewEmoji("👍", "thumbsup"));
        await context.SaveChangesAsync();
        var repository = new Repository(context);

        // Act
        var result = await repository.CreateAsync(
            TestEntities.NewEmoji("👍", "another-name"),
            duplicateCheck: e => e.Symbol == "👍");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateCheck_DoesNotStageNewEntity_WhenMatchExists()
    {
        // Arrange
        await using var context = _db.NewContext();
        context.Emojis.Add(TestEntities.NewEmoji("👍", "thumbsup"));
        await context.SaveChangesAsync();
        var repository = new Repository(context);

        // Act
        await repository.CreateAsync(
            TestEntities.NewEmoji("👍", "another-name"),
            duplicateCheck: e => e.Symbol == "👍");
        await context.SaveChangesAsync();

        // Assert
        await using var verifyContext = _db.NewContext();
        Assert.Single(verifyContext.Emojis);
    }

    [Fact]
    public async Task Update_WithoutSaveChanges_DoesNotPersist()
    {
        // Arrange
        await using var seedContext = _db.NewContext();
        var project = TestEntities.NewProject("Original");
        seedContext.Projects.Add(project);
        await seedContext.SaveChangesAsync();
        var projectId = project.ProjectId;

        await using var actContext = _db.NewContext();
        var tracked = await actContext.Projects.FindAsync(projectId);
        if (tracked != null)
        {
            tracked.OrganisationName = "Modified";
            var repository = new Repository(actContext);

            // Act
            repository.Update(tracked);
        }

        // Assert
        await using var verifyContext = _db.NewContext();
        var stored = await verifyContext.Projects.FindAsync(projectId);
        Assert.Equal("Original", stored?.OrganisationName);
    }

    [Fact]
    public async Task DeleteAsync_WithoutSaveChanges_DoesNotPersist()
    {
        // Arrange
        await using var seedContext = _db.NewContext();
        seedContext.Emojis.Add(TestEntities.NewEmoji());
        await seedContext.SaveChangesAsync();

        await using var actContext = _db.NewContext();
        var repository = new Repository(actContext);

        // Act
        var removed = await repository.DeleteAsync<Emoji>(e => e.Symbol == "👍");

        // Assert
        Assert.True(removed);
        await using var verifyContext = _db.NewContext();
        Assert.Single(verifyContext.Emojis);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenEntityNotFound()
    {
        // Arrange
        await using var context = _db.NewContext();
        var repository = new Repository(context);

        // Act
        var removed = await repository.DeleteAsync<Emoji>(e => e.Symbol == "missing");

        // Assert
        Assert.False(removed);
    }

    [Fact]
    public async Task DeleteByIdAsync_WithoutSaveChanges_DoesNotPersist()
    {
        // Arrange
        await using var seedContext = _db.NewContext();
        var emoji = TestEntities.NewEmoji();
        seedContext.Emojis.Add(emoji);
        await seedContext.SaveChangesAsync();
        var emojiId = emoji.EmojiId;

        await using var actContext = _db.NewContext();
        var repository = new Repository(actContext);

        // Act
        var removed = await repository.DeleteByIdAsync<Emoji>(emojiId);

        // Assert
        Assert.True(removed);
        await using var verifyContext = _db.NewContext();
        Assert.Single(verifyContext.Emojis);
    }

    [Fact]
    public async Task DeleteByIdAsync_ReturnsFalse_WhenEntityNotFound()
    {
        // Arrange
        await using var context = _db.NewContext();
        var repository = new Repository(context);

        // Act
        var removed = await repository.DeleteByIdAsync<Emoji>(9999);

        // Assert
        Assert.False(removed);
    }
}
