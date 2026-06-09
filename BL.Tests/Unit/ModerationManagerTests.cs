using SynapseEnqueteManagement.BL.Domain.I18n.Entities;

namespace SynapseEnqueteManagement.BL.Tests.Unit;

public class ModerationManagerTests
{
    private readonly Mock<IIdeaManager> _ideaManager = new(MockBehavior.Strict);
    private readonly ModerationManager _manager;

    private static readonly Language LanguageNl = new() { Code = "nl", DisplayName = "Nederlands", IsActive = true };

    private const int ProjectId = 1;

    public ModerationManagerTests()
    {
        _manager = new ModerationManager(_ideaManager.Object);
    }

    [Fact]
    public async Task GetIdeasPagedAsync_MapsIdeasAndAttachesQueueCounts()
    {
        var category = new Category
        {
            CategoryId = 9,
            Translations = new List<CategoryTranslation> { new() { Language = LanguageNl, Name = "Cat" } }
        };
        var idea = new Idea
        {
            IdeaId = 77,
            Description = "Full description not truncated here",
            Category = category,
            ImageUrl = "img.png",
            Language = LanguageNl,
            CreatedAt = DateTime.UtcNow,
            Status = ModerationStatus.Unflagged,
            Participant = new Participant { Email = "p@example.com" },
            Comments = new List<Comment>
            {
                new IconComment { TextComment = null },     // reaction on idea
                new IconComment { TextComment = new TextComment() }, // reaction on sub-comment
                new TextComment(),                                   // not a reaction
            }
        };

        _ideaManager.Setup(m => m.GetIdeasPageAsync(ProjectId, "q", 9, ModerationStatus.Unflagged, 2, 25))
            .ReturnsAsync((new List<Idea> { idea }, 1))
            .Verifiable(Times.Once());
        _ideaManager.Setup(m => m.CountByStatusAsync(ProjectId, "q", 9))
            .ReturnsAsync(new Dictionary<ModerationStatus, int>
            {
                [ModerationStatus.Approved]  = 10,
                [ModerationStatus.Unflagged] = 4,
                [ModerationStatus.Flagged]   = 2,
                [ModerationStatus.Declined]  = 3,
            }).Verifiable(Times.Once());

        var (items, total, approved, pending, flagged, declined) =
            await _manager.GetIdeasPagedAsync(ProjectId, "q", 9, ModerationStatus.Unflagged, page: 2, "nl", pageSize: 25);

        Assert.Equal(1, total);
        Assert.Equal(10, approved);
        Assert.Equal(4, pending);   // ModerationStatus.Unflagged maps to pending
        Assert.Equal(2, flagged);
        Assert.Equal(3, declined);

        var item = Assert.Single(items);
        Assert.Equal(77, item.IdeaId);
        Assert.Equal("Full description not truncated here", item.Description); // no truncation in paged listing
        Assert.Equal("Cat", item.CategoryName);
        Assert.Equal(9, item.CategoryId);
        Assert.Equal(2, item.ReactionCount); // all icon comments, both with and without TextComment
        Assert.Equal("img.png", item.ImageUrl);
        Assert.Equal("nl", item.LanguageCode);

        _ideaManager.VerifyAll();
    }

    [Fact]
    public async Task GetIdeasPagedAsync_MissingStatusKeys_FallsBackToZeroCounts()
    {
        _ideaManager.Setup(m => m.GetIdeasPageAsync(ProjectId, null, null, null, 1, 20))
            .ReturnsAsync((new List<Idea>(), 0));
        _ideaManager.Setup(m => m.CountByStatusAsync(ProjectId, null, null))
            .ReturnsAsync(new Dictionary<ModerationStatus, int>());

        var (_, _, approved, pending, flagged, declined) =
            await _manager.GetIdeasPagedAsync(ProjectId, null, null, null, page: 1, "nl");

        Assert.Equal(0, approved);
        Assert.Equal(0, pending);
        Assert.Equal(0, flagged);
        Assert.Equal(0, declined);
    }

    [Fact]
    public async Task SetIdeaModerationStatusAsync_DelegatesToIdeaManager()
    {
        _ideaManager.Setup(m => m.SetModerationStatusAsync(42, ModerationStatus.Approved, ProjectId))
            .Returns(Task.CompletedTask)
            .Verifiable(Times.Once());

        await _manager.SetIdeaModerationStatusAsync(42, ModerationStatus.Approved, ProjectId);

        _ideaManager.VerifyAll();
    }
}
