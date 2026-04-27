namespace SynapseEnqueteManagement.BL.Tests.Unit;

public class CategoryManagerTests
{
    private readonly Mock<ICategoryRepository> _categoryRepo = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _uow = new(MockBehavior.Strict);
    private readonly CategoryManager _manager;

    public CategoryManagerTests()
    {
        _manager = new CategoryManager(_categoryRepo.Object, _uow.Object);
    }

    [Fact]
    public async Task SetIdeaModerationStatusAsync_IdeaExists_UpdatesStatusAndSaves()
    {
        var idea = new Idea { IdeaId = 1, Status = ModerationStatus.Unflagged };
        _categoryRepo.Setup(r => r.ReadIdeaByIdForUpdateAsync(1, It.IsAny<int?>()))
            .ReturnsAsync(idea)
            .Verifiable(Times.Once());
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .Verifiable(Times.Once());

        await _manager.SetIdeaModerationStatusAsync(1, ModerationStatus.Approved);

        Assert.Equal(ModerationStatus.Approved, idea.Status);
        _categoryRepo.VerifyAll();
        _uow.VerifyAll();
    }

    [Fact]
    public async Task SetIdeaModerationStatusAsync_IdeaNotFound_Throws()
    {
        _categoryRepo.Setup(r => r.ReadIdeaByIdForUpdateAsync(99, It.IsAny<int?>()))
            .ReturnsAsync((Idea?)null)
            .Verifiable(Times.Once());

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _manager.SetIdeaModerationStatusAsync(99, ModerationStatus.Approved));

        _categoryRepo.VerifyAll();
    }
}
