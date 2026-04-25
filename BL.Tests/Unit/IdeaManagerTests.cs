using Microsoft.Extensions.Logging.Abstractions;
using SynapseEnqueteManagement.BL.Sorting;

namespace SynapseEnqueteManagement.BL.Tests.Unit;

public class IdeaManagerTests
{
    private readonly Mock<IIdeaRepository> _ideaRepository = new(MockBehavior.Strict);
    private readonly Mock<ICategoryRepository> _categoryRepository = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _uow = new(MockBehavior.Strict);
    private readonly Mock<ISortStrategyFactory<Idea>> _sortFactory = new();
    private readonly IdeaManager _manager;

    private readonly Participant _participant = new() { ParticipantId = 1 };
    private const int CategoryId = 10;
    private const int ProjectId = 100;

    public IdeaManagerTests()
    {
        _manager = new IdeaManager(_ideaRepository.Object, _categoryRepository.Object, _uow.Object, NullLogger<IdeaManager>.Instance, _sortFactory.Object);
    }

    [Fact]
    public async Task AddIdeaAsync_HappyPath_FirstIdeaWithContactInfoRequested_AsksForContact()
    {
        // Arrange
        var category = new Category { CategoryId = CategoryId, Project = new Project { ProjectId = ProjectId, RequestContactInfo = true } };
        _categoryRepository.Setup(r => r.ReadByIdWithProjectForUpdateAsync(CategoryId))
            .ReturnsAsync(category)
            .Verifiable(Times.Exactly(1));
        _ideaRepository.Setup(r => r.AnyExistByParticipantAsync(It.IsAny<int>()))
            .ReturnsAsync(false);
        _ideaRepository.Setup(r => r.CreateAsync(It.IsAny<Idea>()))
            .ReturnsAsync((Idea i) => { i.IdeaId = 7; return i; })
            .Verifiable(Times.Exactly(1));
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .Verifiable(Times.Exactly(1));

        // Act
        var result = await _manager.AddIdeaAsync(CategoryId, ProjectId, "desc", ModerationStatus.Approved, _participant);

        // Assert
        Assert.Equal(7, result.IdeaId);
        Assert.True(result.ShouldAskForContactInfo);
        _categoryRepository.VerifyAll();
        _ideaRepository.VerifyAll();
        _uow.VerifyAll();
    }

    [Fact]
    public async Task AddIdeaAsync_HappyPath_NotFirstIdea_DoesNotAskForContact()
    {
        // Arrange
        var category = new Category { CategoryId = CategoryId, Project = new Project { ProjectId = ProjectId, RequestContactInfo = true } };
        _categoryRepository.Setup(r => r.ReadByIdWithProjectForUpdateAsync(CategoryId)).ReturnsAsync(category);
        _ideaRepository.Setup(r => r.AnyExistByParticipantAsync(It.IsAny<int>())).ReturnsAsync(true);
        _ideaRepository.Setup(r => r.CreateAsync(It.IsAny<Idea>()))
            .ReturnsAsync((Idea i) => i);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _manager.AddIdeaAsync(CategoryId, ProjectId, "desc", ModerationStatus.Approved, _participant);

        // Assert
        Assert.False(result.ShouldAskForContactInfo);
    }

    [Fact]
    public async Task AddIdeaAsync_HappyPath_FirstIdeaButProjectDoesNotRequestContact_DoesNotAskForContact()
    {
        // Arrange
        var category = new Category { CategoryId = CategoryId, Project = new Project { ProjectId = ProjectId, RequestContactInfo = false } };
        _categoryRepository.Setup(r => r.ReadByIdWithProjectForUpdateAsync(CategoryId)).ReturnsAsync(category);
        _ideaRepository.Setup(r => r.AnyExistByParticipantAsync(It.IsAny<int>())).ReturnsAsync(false);
        _ideaRepository.Setup(r => r.CreateAsync(It.IsAny<Idea>()))
            .ReturnsAsync((Idea i) => i);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _manager.AddIdeaAsync(CategoryId, ProjectId, "desc", ModerationStatus.Approved, _participant);

        // Assert
        Assert.False(result.ShouldAskForContactInfo);
    }

    [Fact]
    public async Task AddIdeaAsync_ForwardsCancellationTokenToUnitOfWork()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;
        var category = new Category { CategoryId = CategoryId, Project = new Project { ProjectId = ProjectId } };
        _categoryRepository.Setup(r => r.ReadByIdWithProjectForUpdateAsync(CategoryId))
            .ReturnsAsync(category);
        _ideaRepository.Setup(r => r.AnyExistByParticipantAsync(It.IsAny<int>())).ReturnsAsync(false);
        _ideaRepository.Setup(r => r.CreateAsync(It.IsAny<Idea>()))
            .ReturnsAsync((Idea i) => i);
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
        _categoryRepository.Setup(r => r.ReadByIdWithProjectForUpdateAsync(CategoryId))
            .ReturnsAsync((Category)null)
            .Verifiable(Times.Exactly(1));

        // Act + Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _manager.AddIdeaAsync(CategoryId, ProjectId, "desc", ModerationStatus.Approved, _participant));

        _categoryRepository.VerifyAll();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddIdeaAsync_CategoryBelongsToDifferentProject_ThrowsAndDoesNotSave()
    {
        // Arrange
        var category = new Category { CategoryId = CategoryId, Project = new Project { ProjectId = ProjectId + 1 } };
        _categoryRepository.Setup(r => r.ReadByIdWithProjectForUpdateAsync(CategoryId))
            .ReturnsAsync(category)
            .Verifiable(Times.Exactly(1));

        // Act + Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _manager.AddIdeaAsync(CategoryId, ProjectId, "desc", ModerationStatus.Approved, _participant));

        _categoryRepository.VerifyAll();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
