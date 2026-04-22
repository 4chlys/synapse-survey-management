namespace SynapseEnqueteManagement.DAL.Tests.Integration;

public class UnitOfWorkTests : IDisposable
{
    private readonly InMemoryDatabase _db = new();

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task SaveChangesAsync_PersistsStagedChanges()
    {
        // Arrange
        await using var context = _db.NewContext();
        var repository = new Repository(context);
        var uow = new UnitOfWork(context);
        await repository.CreateAsync(TestEntities.NewEmoji());

        // Act
        await uow.SaveChangesAsync();

        // Assert
        await using var verifyContext = _db.NewContext();
        Assert.Single(verifyContext.Emojis);
    }

    [Fact]
    public async Task SaveChangesAsync_ReturnsNumberOfAffectedRows()
    {
        // Arrange
        await using var context = _db.NewContext();
        var repository = new Repository(context);
        var uow = new UnitOfWork(context);
        await repository.CreateAsync(TestEntities.NewEmoji("👍", "thumbsup"));
        await repository.CreateAsync(TestEntities.NewEmoji("❤️", "heart"));

        // Act
        var affected = await uow.SaveChangesAsync();

        // Assert
        Assert.Equal(2, affected);
    }

    [Fact]
    public async Task CommitAsync_PersistsStagedChanges_WhenTransactionIsActive()
    {
        // Arrange
        await using var context = _db.NewContext();
        var repository = new Repository(context);
        var uow = new UnitOfWork(context);

        // Act
        await uow.BeginTransactionAsync();
        await repository.CreateAsync(TestEntities.NewEmoji());
        await uow.CommitAsync();

        // Assert
        await using var verifyContext = _db.NewContext();
        Assert.Single(verifyContext.Emojis);
    }

    [Fact]
    public async Task RollbackAsync_DiscardsStagedChanges_WhenTransactionIsActive()
    {
        // Arrange
        await using var context = _db.NewContext();
        var repository = new Repository(context);
        var uow = new UnitOfWork(context);

        // Act
        await uow.BeginTransactionAsync();
        await repository.CreateAsync(TestEntities.NewEmoji());
        await uow.SaveChangesAsync();
        await uow.RollbackAsync();

        // Assert
        await using var verifyContext = _db.NewContext();
        Assert.Empty(verifyContext.Emojis);
    }

    [Fact]
    public async Task BeginTransactionAsync_Throws_WhenTransactionAlreadyActive()
    {
        // Arrange
        await using var context = _db.NewContext();
        var uow = new UnitOfWork(context);
        await uow.BeginTransactionAsync();

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => uow.BeginTransactionAsync());

        await uow.RollbackAsync();
    }

    [Fact]
    public async Task CommitAsync_Throws_WhenNoTransactionActive()
    {
        // Arrange
        await using var context = _db.NewContext();
        var uow = new UnitOfWork(context);

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => uow.CommitAsync());
    }

    [Fact]
    public async Task RollbackAsync_IsNoOp_WhenNoTransactionActive()
    {
        // Arrange
        await using var context = _db.NewContext();
        var uow = new UnitOfWork(context);

        // Act
        await uow.RollbackAsync();

        // Assert — reaching this line means no exception was thrown.
        Assert.True(true);
    }

    [Fact]
    public async Task SaveChangesAsync_WithPreCancelledToken_ThrowsAndDoesNotPersist()
    {
        // Arrange
        await using var context = _db.NewContext();
        var repository = new Repository(context);
        var uow = new UnitOfWork(context);
        await repository.CreateAsync(TestEntities.NewEmoji());
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act + Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => uow.SaveChangesAsync(cts.Token));

        await using var verifyContext = _db.NewContext();
        Assert.Empty(verifyContext.Emojis);
    }
}
