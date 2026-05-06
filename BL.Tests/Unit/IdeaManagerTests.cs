using Microsoft.Extensions.Logging.Abstractions;
using SynapseEnqueteManagement.BL.Embeddings;
using SynapseEnqueteManagement.BL.Sorting;
using SynapseEnqueteManagement.BL.Sorting.Embeddings;

namespace SynapseEnqueteManagement.BL.Tests.Unit;

public class IdeaManagerTests
{
    private readonly Mock<IIdeaRepository> _ideaRepository = new(MockBehavior.Strict);
    private readonly Mock<ICategoryRepository> _categoryRepository = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _uow = new(MockBehavior.Strict);
    private readonly Mock<ISortStrategyFactory<Idea>> _sortFactory = new();
    private readonly Mock<IEmbeddingSortStrategyFactory> _embeddingSortFactory = new();
    private readonly Mock<IEmbeddingService> _embeddingService = new();
    private readonly IdeaManager _manager;

    private readonly Participant _participant = new() { ParticipantId = 1 };
    private const int CategoryId = 10;
    private const int ProjectId = 100;

    public IdeaManagerTests()
    {
        _manager = new IdeaManager(_ideaRepository.Object, _categoryRepository.Object, _uow.Object, NullLogger<IdeaManager>.Instance, _sortFactory.Object, _embeddingSortFactory.Object, _embeddingService.Object);
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
        await _manager.AddIdeaAsync(CategoryId, ProjectId, "desc", ModerationStatus.Approved, _participant, ct: ct);

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

    [Fact]
    public async Task GetIdeasIndexAsync_CrossLanguageFalse_FilterAppliedEvenWhenShowAllTrue()
    {
        // Arrange
        _ideaRepository.Setup(r => r.ReadProjectFlagsForCategoryReadOnlyAsync(CategoryId))
            .ReturnsAsync((false, "nl"));
        _ideaRepository.Setup(r => r.ReadMostRecentDescriptionByCategoryAndParticipantAsync(CategoryId, 1))
            .ReturnsAsync((string)null);
        _embeddingSortFactory.Setup(f => f.IsEmbeddingKey(It.IsAny<string>())).Returns(false);
        var strategy = new Mock<ISortStrategy<Idea>>();
        strategy.Setup(s => s.DbOrderBy).Returns(q => q.OrderBy(i => i.IdeaId));
        strategy.Setup(s => s.Randomize).Returns(false);
        _sortFactory.Setup(f => f.Get(It.IsAny<string>())).Returns(strategy.Object);
        _ideaRepository.Setup(r => r.CountVisibleByCategoryAsync(CategoryId, "nl")).ReturnsAsync(0).Verifiable();
        _ideaRepository.Setup(r => r.ReadVisibleByCategoryWithCommentsReadOnlyAsync(
            CategoryId, It.IsAny<Func<IQueryable<Idea>, IOrderedQueryable<Idea>>>(), false, 0, 5, "nl"))
            .ReturnsAsync(new List<Idea>()).Verifiable();

        // Act
        await _manager.GetIdeasIndexAsync(CategoryId, 1, 5, 1, "diverse", "nl", showAllLanguages: true);

        // Assert — filter is "nl", not null, because AllowCrossLanguageIdeas is false
        _ideaRepository.Verify(r => r.CountVisibleByCategoryAsync(CategoryId, "nl"), Times.Once);
        _ideaRepository.Verify(r => r.ReadVisibleByCategoryWithCommentsReadOnlyAsync(
            CategoryId, It.IsAny<Func<IQueryable<Idea>, IOrderedQueryable<Idea>>>(), false, 0, 5, "nl"), Times.Once);
    }

    [Fact]
    public async Task GetIdeasIndexAsync_CrossLanguageTrue_ShowAllFalse_FilteredToParticipantLanguage()
    {
        // Arrange
        _ideaRepository.Setup(r => r.ReadProjectFlagsForCategoryReadOnlyAsync(CategoryId))
            .ReturnsAsync((true, "nl"));
        _ideaRepository.Setup(r => r.ReadMostRecentDescriptionByCategoryAndParticipantAsync(CategoryId, 1))
            .ReturnsAsync((string)null);
        _embeddingSortFactory.Setup(f => f.IsEmbeddingKey(It.IsAny<string>())).Returns(false);
        var strategy = new Mock<ISortStrategy<Idea>>();
        strategy.Setup(s => s.DbOrderBy).Returns(q => q.OrderBy(i => i.IdeaId));
        strategy.Setup(s => s.Randomize).Returns(false);
        _sortFactory.Setup(f => f.Get(It.IsAny<string>())).Returns(strategy.Object);
        _ideaRepository.Setup(r => r.CountVisibleByCategoryAsync(CategoryId, "en")).ReturnsAsync(0).Verifiable();
        _ideaRepository.Setup(r => r.ReadVisibleByCategoryWithCommentsReadOnlyAsync(
            CategoryId, It.IsAny<Func<IQueryable<Idea>, IOrderedQueryable<Idea>>>(), false, 0, 5, "en"))
            .ReturnsAsync(new List<Idea>()).Verifiable();

        // Act
        await _manager.GetIdeasIndexAsync(CategoryId, 1, 5, 1, "diverse", "en", showAllLanguages: false);

        // Assert — filter is participant's language "en"
        _ideaRepository.Verify(r => r.CountVisibleByCategoryAsync(CategoryId, "en"), Times.Once);
        _ideaRepository.Verify(r => r.ReadVisibleByCategoryWithCommentsReadOnlyAsync(
            CategoryId, It.IsAny<Func<IQueryable<Idea>, IOrderedQueryable<Idea>>>(), false, 0, 5, "en"), Times.Once);
    }

    [Fact]
    public async Task GetIdeasIndexAsync_CrossLanguageTrue_ShowAllTrue_NoFilter()
    {
        // Arrange
        _ideaRepository.Setup(r => r.ReadProjectFlagsForCategoryReadOnlyAsync(CategoryId))
            .ReturnsAsync((true, "nl"));
        _ideaRepository.Setup(r => r.ReadMostRecentDescriptionByCategoryAndParticipantAsync(CategoryId, 1))
            .ReturnsAsync((string)null);
        _embeddingSortFactory.Setup(f => f.IsEmbeddingKey(It.IsAny<string>())).Returns(false);
        var strategy = new Mock<ISortStrategy<Idea>>();
        strategy.Setup(s => s.DbOrderBy).Returns(q => q.OrderBy(i => i.IdeaId));
        strategy.Setup(s => s.Randomize).Returns(false);
        _sortFactory.Setup(f => f.Get(It.IsAny<string>())).Returns(strategy.Object);
        _ideaRepository.Setup(r => r.CountVisibleByCategoryAsync(CategoryId, null)).ReturnsAsync(0).Verifiable();
        _ideaRepository.Setup(r => r.ReadVisibleByCategoryWithCommentsReadOnlyAsync(
            CategoryId, It.IsAny<Func<IQueryable<Idea>, IOrderedQueryable<Idea>>>(), false, 0, 5, null))
            .ReturnsAsync(new List<Idea>()).Verifiable();

        // Act
        await _manager.GetIdeasIndexAsync(CategoryId, 1, 5, 1, "diverse", "en", showAllLanguages: true);

        // Assert — no filter (null) because AllowCrossLanguageIdeas is true and showAllLanguages is true
        _ideaRepository.Verify(r => r.CountVisibleByCategoryAsync(CategoryId, null), Times.Once);
        _ideaRepository.Verify(r => r.ReadVisibleByCategoryWithCommentsReadOnlyAsync(
            CategoryId, It.IsAny<Func<IQueryable<Idea>, IOrderedQueryable<Idea>>>(), false, 0, 5, null), Times.Once);
    }

    [Fact]
    public async Task AddIdeaAsync_StampsParticipantLanguageOnIdea()
    {
        // Arrange
        var language = new Language { Code = "nl" };
        var participant = new Participant { ParticipantId = 1, Language = language };
        var category = new Category { CategoryId = CategoryId, Project = new Project { ProjectId = ProjectId, RequestContactInfo = false } };
        _categoryRepository.Setup(r => r.ReadByIdWithProjectForUpdateAsync(CategoryId)).ReturnsAsync(category);
        _ideaRepository.Setup(r => r.AnyExistByParticipantAsync(It.IsAny<int>())).ReturnsAsync(false);
        Idea captured = null;
        _ideaRepository.Setup(r => r.CreateAsync(It.IsAny<Idea>()))
            .Callback<Idea>(i => captured = i)
            .ReturnsAsync((Idea i) => i);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        await _manager.AddIdeaAsync(CategoryId, ProjectId, "desc", ModerationStatus.Approved, participant);

        // Assert
        Assert.Same(language, captured.Language);
    }
}
