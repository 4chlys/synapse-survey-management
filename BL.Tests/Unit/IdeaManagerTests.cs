using System.Linq.Expressions;
using Microsoft.Extensions.Logging.Abstractions;
using SynapseEnqueteManagement.BL.Sorting;

namespace SynapseEnqueteManagement.BL.Tests.Unit;

public class IdeaManagerTests
{
    private readonly Mock<IRepository> _repository = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _uow = new(MockBehavior.Strict);
    private readonly Mock<ISortStrategyFactory<Idea>> _sortFactory = new();
    private readonly IdeaManager _manager;

    private readonly Participant _participant = new() { ParticipantId = 1 };
    private const int CategoryId = 10;
    private const int ProjectId = 100;

    public IdeaManagerTests()
    {
        _manager = new IdeaManager(_repository.Object, _uow.Object, NullLogger<IdeaManager>.Instance, _sortFactory.Object);
    }

    [Fact]
    public async Task AddIdeaAsync_HappyPath_CreatesIdeaAndSavesOnce()
    {
        // Arrange
        var category = new Category { CategoryId = CategoryId, Project = new Project { ProjectId = ProjectId } };
        _repository.Setup(r => r.ReadByIdAsync<Category>(CategoryId, It.IsAny<Expression<Func<Category, object>>[]>(), It.IsAny<string[]>(), It.IsAny<bool>()))
            .ReturnsAsync(category)
            .Verifiable(Times.Exactly(1));
        _repository.Setup(r => r.CreateAsync(It.IsAny<Idea>(), It.IsAny<Expression<Func<Idea, bool>>>()))
            .ReturnsAsync((Idea i, Expression<Func<Idea, bool>> _) => i)
            .Verifiable(Times.Exactly(1));
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .Verifiable(Times.Exactly(1));

        // Act
        var idea = await _manager.AddIdeaAsync(CategoryId, ProjectId, "desc", ModerationStatus.Approved, _participant);

        // Assert
        Assert.NotNull(idea);
        Assert.Equal("desc", idea.Description);
        Assert.Same(category, idea.Category);
        Assert.Same(_participant, idea.Participant);
        _repository.VerifyAll();
        _uow.VerifyAll();
    }

    [Fact]
    public async Task AddIdeaAsync_ForwardsCancellationTokenToUnitOfWork()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;
        var category = new Category { CategoryId = CategoryId, Project = new Project { ProjectId = ProjectId } };
        _repository.Setup(r => r.ReadByIdAsync<Category>(CategoryId, It.IsAny<Expression<Func<Category, object>>[]>(), It.IsAny<string[]>(), It.IsAny<bool>()))
            .ReturnsAsync(category);
        _repository.Setup(r => r.CreateAsync(It.IsAny<Idea>(), It.IsAny<Expression<Func<Idea, bool>>>()))
            .ReturnsAsync((Idea i, Expression<Func<Idea, bool>> _) => i);
        _uow.Setup(u => u.SaveChangesAsync(ct))
            .ReturnsAsync(1)
            .Verifiable(Times.Exactly(1));

        // Act
        await _manager.AddIdeaAsync(CategoryId, ProjectId, "desc", ModerationStatus.Approved, _participant, ct);

        // Assert
        _uow.VerifyAll();
    }

    [Fact]
    public async Task AddIdeaAsync_CategoryNotFound_ThrowsAndDoesNotSave()
    {
        // Arrange
        _repository.Setup(r => r.ReadByIdAsync<Category>(CategoryId, It.IsAny<Expression<Func<Category, object>>[]>(), It.IsAny<string[]>(), It.IsAny<bool>()))
            .ReturnsAsync((Category)null)
            .Verifiable(Times.Exactly(1));

        // Act + Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _manager.AddIdeaAsync(CategoryId, ProjectId, "desc", ModerationStatus.Approved, _participant));

        _repository.VerifyAll();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddIdeaAsync_CategoryBelongsToDifferentProject_ThrowsAndDoesNotSave()
    {
        // Arrange
        var category = new Category { CategoryId = CategoryId, Project = new Project { ProjectId = ProjectId + 1 } };
        _repository.Setup(r => r.ReadByIdAsync<Category>(CategoryId, It.IsAny<Expression<Func<Category, object>>[]>(), It.IsAny<string[]>(), It.IsAny<bool>()))
            .ReturnsAsync(category)
            .Verifiable(Times.Exactly(1));

        // Act + Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _manager.AddIdeaAsync(CategoryId, ProjectId, "desc", ModerationStatus.Approved, _participant));

        _repository.VerifyAll();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
