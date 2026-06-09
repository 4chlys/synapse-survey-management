using SynapseEnqueteManagement.BL.Domain.I18n.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Stats;

namespace SynapseEnqueteManagement.BL.Tests.Unit;

public class DashboardManagerTests
{
    private readonly Mock<ICategoryManager> _categoryManager = new(MockBehavior.Strict);
    private readonly Mock<IIdeaManager> _ideaManager = new(MockBehavior.Strict);
    private readonly DashboardManager _manager;

    private static readonly Language LanguageNl = new() { Code = "nl", DisplayName = "Nederlands", IsActive = true };

    private const int ProjectId = 1;

    public DashboardManagerTests()
    {
        _manager = new DashboardManager(_categoryManager.Object, _ideaManager.Object);
    }

    [Fact]
    public async Task GetDashboardMetricsAsync_ComposesCategoryAndIdeaManagers_AndTruncatesLongDescriptions()
    {
        var categories = new List<CategoryMetric>
        {
            new(1, "Cat A", IdeaCount: 3, TotalReactions: 7),
        };
        var longDesc = new string('x', 150);
        var ideas = new List<IdeaMetric>
        {
            new(IdeaId: 10, Description: longDesc, ReactionCount: 4, CommentCount: 1),
            new(IdeaId: 11, Description: "short",  ReactionCount: 0, CommentCount: 0),
        };

        _categoryManager.Setup(m => m.GetTopCategoriesAsync(ProjectId, 5, "nl", "nl"))
            .ReturnsAsync(categories).Verifiable(Times.Once());
        _ideaManager.Setup(m => m.GetTopIdeasAsync(ProjectId, 5))
            .ReturnsAsync(ideas).Verifiable(Times.Once());

        var result = await _manager.GetDashboardMetricsAsync(ProjectId, "nl", "nl");

        Assert.Same(categories, result.TopCategories);
        Assert.Equal(2, result.TopIdeas.Count);
        // Long description truncated to 100 chars + ellipsis
        Assert.Equal(new string('x', 100) + "…", result.TopIdeas[0].Description);
        Assert.Equal(10, result.TopIdeas[0].IdeaId);
        // Short one passes through unchanged
        Assert.Equal("short", result.TopIdeas[1].Description);

        _categoryManager.VerifyAll();
        _ideaManager.VerifyAll();
    }

    [Fact]
    public async Task GetIdeaFeedAsync_MapsIdeasToFeedItems_CountsReactionsExcludingSubCommentReactions()
    {
        var category = new Category
        {
            CategoryId = 4,
            Translations = new List<CategoryTranslation> { new() { Language = LanguageNl, Name = "Cat" } }
        };
        var participant = new Participant { Email = "p@example.com" };
        var subText = new TextComment();
        var ideas = new List<Idea>
        {
            new()
            {
                IdeaId = 50, Description = new string('a', 120),
                Category = category, Participant = participant,
                CreatedAt = DateTime.UtcNow, Status = ModerationStatus.Approved,
                Comments = new List<Comment>
                {
                    new IconComment { TextComment = null },          // reaction on idea  → counted
                    new IconComment { TextComment = subText },       // reaction on sub-comment → NOT counted
                    new TextComment(),                                // text comment, not a reaction
                }
            }
        };

        _ideaManager.Setup(m => m.GetLatestApprovedWithCommentsAsync(ProjectId, 10))
            .ReturnsAsync(ideas).Verifiable(Times.Once());

        var result = await _manager.GetIdeaFeedAsync(ProjectId, "nl");

        var item = Assert.Single(result);
        Assert.Equal(50, item.IdeaId);
        Assert.Equal(new string('a', 80) + "…", item.Description); // 80-char truncation
        Assert.Equal("Cat", item.CategoryName);
        Assert.Equal(4, item.CategoryId);
        Assert.Equal(1, item.ReactionCount);                      // only the TextComment-null IconComment
        Assert.Equal("p@example.com", item.ParticipantEmail);

        _ideaManager.VerifyAll();
    }

    [Fact]
    public async Task GetModerationQueueAsync_MapsStatusCountsToResult()
    {
        var counts = new Dictionary<ModerationStatus, int>
        {
            [ModerationStatus.Approved] = 3,
            [ModerationStatus.Flagged]  = 2,
            [ModerationStatus.Declined] = 1,
            // Unflagged intentionally omitted to exercise the default-zero fallback
        };

        _ideaManager.Setup(m => m.CountByStatusAsync(ProjectId, null, null))
            .ReturnsAsync(counts).Verifiable(Times.Once());

        var result = await _manager.GetModerationQueueAsync(ProjectId);

        Assert.Equal(3, result.Approved);
        Assert.Equal(2, result.Flagged);
        Assert.Equal(1, result.Declined);
        Assert.Equal(0, result.Unflagged);

        _ideaManager.VerifyAll();
    }

    [Fact]
    public async Task GetModerationQueueAsync_EmptyCounts_ReturnsAllZeros()
    {
        _ideaManager.Setup(m => m.CountByStatusAsync(ProjectId, null, null))
            .ReturnsAsync(new Dictionary<ModerationStatus, int>());

        var result = await _manager.GetModerationQueueAsync(ProjectId);

        Assert.Equal(0, result.Approved);
        Assert.Equal(0, result.Flagged);
        Assert.Equal(0, result.Declined);
        Assert.Equal(0, result.Unflagged);
    }
}
