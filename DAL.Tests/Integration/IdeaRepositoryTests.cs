using SynapseEnqueteManagement.BL.Domain.I18n.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.DAL.Tests.Integration;

public class IdeaRepositoryTests : IDisposable
{
    private readonly InMemoryDatabase _db = new();

    public void Dispose() => _db.Dispose();

    private async Task<(Category Category, Participant Participant)> SeedAsync(
        Language language = null)
    {
        await using var ctx = _db.NewContext();

        language ??= TestEntities.NewLanguage();
        ctx.Languages.Add(language);

        var project     = TestEntities.NewProject(language);
        var category    = TestEntities.NewCategory(project);
        var participant = TestEntities.NewParticipant(project, language);

        ctx.Projects.Add(project);
        ctx.Categories.Add(category);
        ctx.Participants.Add(participant);

        await ctx.SaveChangesAsync();
        return (category, participant);
    }

    [Fact]
    public async Task CountVisibleByCategoriesAsync_ApprovedIdeas_ReturnedInCount()
    {
        // Arrange
        var (cat, participant) = await SeedAsync();

        await using var seedCtx = _db.NewContext();
        var trackedCat         = await seedCtx.Categories.FindAsync(cat.CategoryId);
        var trackedParticipant = await seedCtx.Participants.FindAsync(participant.ParticipantId);
        seedCtx.Ideas.Add(TestEntities.NewIdea(trackedCat, trackedParticipant, ModerationStatus.Approved));
        seedCtx.Ideas.Add(TestEntities.NewIdea(trackedCat, trackedParticipant, ModerationStatus.Approved));
        await seedCtx.SaveChangesAsync();

        // Act
        await using var actCtx = _db.NewContext();
        var result = await new IdeaRepository(actCtx)
            .CountVisibleByCategoriesAsync(new[] { cat.CategoryId });

        // Assert
        Assert.Equal(2, result.GetValueOrDefault(cat.CategoryId));
    }

    [Fact]
    public async Task CountVisibleByCategoriesAsync_FlaggedIdeas_ExcludedFromCount()
    {
        // Arrange
        var (cat, participant) = await SeedAsync();

        await using var seedCtx = _db.NewContext();
        var trackedCat         = await seedCtx.Categories.FindAsync(cat.CategoryId);
        var trackedParticipant = await seedCtx.Participants.FindAsync(participant.ParticipantId);
        seedCtx.Ideas.Add(TestEntities.NewIdea(trackedCat, trackedParticipant, ModerationStatus.Approved));
        seedCtx.Ideas.Add(TestEntities.NewIdea(trackedCat, trackedParticipant, ModerationStatus.Flagged));
        await seedCtx.SaveChangesAsync();

        // Act
        await using var actCtx = _db.NewContext();
        var result = await new IdeaRepository(actCtx)
            .CountVisibleByCategoriesAsync(new[] { cat.CategoryId });

        // Assert — only the Approved idea is counted
        Assert.Equal(1, result.GetValueOrDefault(cat.CategoryId));
    }

    [Fact]
    public async Task CountVisibleByCategoriesAsync_DeclinedIdeas_ExcludedFromCount()
    {
        // Arrange
        var (cat, participant) = await SeedAsync();

        await using var seedCtx = _db.NewContext();
        var trackedCat         = await seedCtx.Categories.FindAsync(cat.CategoryId);
        var trackedParticipant = await seedCtx.Participants.FindAsync(participant.ParticipantId);
        seedCtx.Ideas.Add(TestEntities.NewIdea(trackedCat, trackedParticipant, ModerationStatus.Approved));
        seedCtx.Ideas.Add(TestEntities.NewIdea(trackedCat, trackedParticipant, ModerationStatus.Declined));
        await seedCtx.SaveChangesAsync();

        // Act
        await using var actCtx = _db.NewContext();
        var result = await new IdeaRepository(actCtx)
            .CountVisibleByCategoriesAsync(new[] { cat.CategoryId });

        // Assert — only the Approved idea is counted
        Assert.Equal(1, result.GetValueOrDefault(cat.CategoryId));
    }

    [Fact]
    public async Task CountVisibleByCategoriesAsync_WithLanguageFilter_OnlyMatchingLanguageCounted()
    {
        // Arrange — two languages, two ideas (one per language)
        await using var seedCtx = _db.NewContext();

        var nl = TestEntities.NewLanguage("nl", "Nederlands");
        var en = new Language { Code = "en", DisplayName = "English", IsActive = true };
        seedCtx.Languages.AddRange(nl, en);

        var project     = TestEntities.NewProject(nl);
        var category    = TestEntities.NewCategory(project);
        var participant = TestEntities.NewParticipant(project, nl);

        seedCtx.Projects.Add(project);
        seedCtx.Categories.Add(category);
        seedCtx.Participants.Add(participant);
        await seedCtx.SaveChangesAsync();

        var trackedCat         = await seedCtx.Categories.FindAsync(category.CategoryId);
        var trackedParticipant = await seedCtx.Participants.FindAsync(participant.ParticipantId);
        var trackedNl          = await seedCtx.Languages.FindAsync("nl");
        var trackedEn          = await seedCtx.Languages.FindAsync("en");

        seedCtx.Ideas.Add(TestEntities.NewIdea(trackedCat, trackedParticipant, ModerationStatus.Approved, trackedNl));
        seedCtx.Ideas.Add(TestEntities.NewIdea(trackedCat, trackedParticipant, ModerationStatus.Approved, trackedEn));
        await seedCtx.SaveChangesAsync();

        // Act
        await using var actCtx = _db.NewContext();
        var result = await new IdeaRepository(actCtx)
            .CountVisibleByCategoriesAsync(new[] { category.CategoryId }, filterLanguageCode: "nl");

        // Assert — only the "nl" idea is counted
        Assert.Equal(1, result.GetValueOrDefault(category.CategoryId));
    }

    [Fact]
    public async Task CountVisibleByCategoriesAsync_CategoryNotInList_NotIncludedInResult()
    {
        // Arrange
        var (cat, participant) = await SeedAsync();

        await using var seedCtx = _db.NewContext();
        var trackedCat         = await seedCtx.Categories.FindAsync(cat.CategoryId);
        var trackedParticipant = await seedCtx.Participants.FindAsync(participant.ParticipantId);
        seedCtx.Ideas.Add(TestEntities.NewIdea(trackedCat, trackedParticipant));
        await seedCtx.SaveChangesAsync();

        // Act — ask for a category that does not exist
        await using var actCtx = _db.NewContext();
        var result = await new IdeaRepository(actCtx)
            .CountVisibleByCategoriesAsync(new[] { cat.CategoryId + 999 });

        // Assert — result is empty (no key for the non-existent category)
        Assert.Empty(result);
    }

    // ── ReadIdeasPagedAsync ────────────────────────────────────────────────

    [Fact]
    public async Task ReadIdeasPagedAsync_FiltersByProjectId_ExcludesOtherProjects()
    {
        var (cat, participant) = await SeedAsync();

        // Seed a second independent project with its own idea
        await using var seedCtx = _db.NewContext();
        var lang2        = TestEntities.NewLanguage("en", "English");
        var project2     = TestEntities.NewProject(lang2, "Other Project");
        project2.ShortForm = "other-project";
        var cat2         = TestEntities.NewCategory(project2);
        var participant2 = TestEntities.NewParticipant(project2, lang2);
        seedCtx.Languages.Add(lang2);
        seedCtx.Projects.Add(project2);
        seedCtx.Categories.Add(cat2);
        seedCtx.Participants.Add(participant2);
        await seedCtx.SaveChangesAsync();

        var trackedCat         = await seedCtx.Categories.FindAsync(cat.CategoryId);
        var trackedParticipant = await seedCtx.Participants.FindAsync(participant.ParticipantId);
        seedCtx.Ideas.Add(TestEntities.NewIdea(trackedCat,  trackedParticipant));
        seedCtx.Ideas.Add(TestEntities.NewIdea(cat2, participant2));
        await seedCtx.SaveChangesAsync();

        await using var actCtx = _db.NewContext();
        var (items, total) = await new IdeaRepository(actCtx)
            .ReadIdeasPagedAsync(cat.Project.ProjectId, "", null, null, 1, 20);

        Assert.Equal(1, total);
        Assert.Single(items);
    }

    [Fact]
    public async Task ReadIdeasPagedAsync_SearchFilter_MatchesDescriptionSubstring()
    {
        var (cat, participant) = await SeedAsync();

        await using var seedCtx = _db.NewContext();
        var trackedCat         = await seedCtx.Categories.FindAsync(cat.CategoryId);
        var trackedParticipant = await seedCtx.Participants.FindAsync(participant.ParticipantId);
        var match    = TestEntities.NewIdea(trackedCat, trackedParticipant);
        match.Description = "bicycle lane improvement";
        var noMatch  = TestEntities.NewIdea(trackedCat, trackedParticipant);
        noMatch.Description = "park bench replacement";
        seedCtx.Ideas.AddRange(match, noMatch);
        await seedCtx.SaveChangesAsync();

        await using var actCtx = _db.NewContext();
        var (items, total) = await new IdeaRepository(actCtx)
            .ReadIdeasPagedAsync(cat.Project.ProjectId, "bicycle", null, null, 1, 20);

        Assert.Equal(1, total);
        Assert.Contains(items, i => i.Description.Contains("bicycle"));
    }

    [Fact]
    public async Task ReadIdeasPagedAsync_CategoryIdFilter_ExcludesOtherCategories()
    {
        var (cat, participant) = await SeedAsync();

        await using var seedCtx = _db.NewContext();
        var trackedProject     = await seedCtx.Projects.FindAsync(cat.Project.ProjectId);
        var cat2               = TestEntities.NewCategory(trackedProject);
        seedCtx.Categories.Add(cat2);
        await seedCtx.SaveChangesAsync();

        var trackedCat         = await seedCtx.Categories.FindAsync(cat.CategoryId);
        var trackedParticipant = await seedCtx.Participants.FindAsync(participant.ParticipantId);
        seedCtx.Ideas.Add(TestEntities.NewIdea(trackedCat, trackedParticipant));
        seedCtx.Ideas.Add(TestEntities.NewIdea(cat2, trackedParticipant));
        await seedCtx.SaveChangesAsync();

        await using var actCtx = _db.NewContext();
        var (items, total) = await new IdeaRepository(actCtx)
            .ReadIdeasPagedAsync(cat.Project.ProjectId, "", cat.CategoryId, null, 1, 20);

        Assert.Equal(1, total);
        Assert.All(items, i => Assert.Equal(cat.CategoryId, i.Category.CategoryId));
    }

    [Fact]
    public async Task ReadIdeasPagedAsync_StatusFilter_ExcludesOtherStatuses()
    {
        var (cat, participant) = await SeedAsync();

        await using var seedCtx = _db.NewContext();
        var trackedCat         = await seedCtx.Categories.FindAsync(cat.CategoryId);
        var trackedParticipant = await seedCtx.Participants.FindAsync(participant.ParticipantId);
        seedCtx.Ideas.Add(TestEntities.NewIdea(trackedCat, trackedParticipant, ModerationStatus.Approved));
        seedCtx.Ideas.Add(TestEntities.NewIdea(trackedCat, trackedParticipant, ModerationStatus.Flagged));
        seedCtx.Ideas.Add(TestEntities.NewIdea(trackedCat, trackedParticipant, ModerationStatus.Unflagged));
        await seedCtx.SaveChangesAsync();

        await using var actCtx = _db.NewContext();
        var (items, total) = await new IdeaRepository(actCtx)
            .ReadIdeasPagedAsync(cat.Project.ProjectId, "", null, ModerationStatus.Flagged, 1, 20);

        Assert.Equal(1, total);
        Assert.All(items, i => Assert.Equal(ModerationStatus.Flagged, i.Status));
    }

    [Fact]
    public async Task ReadIdeasPagedAsync_Paging_ReturnsCorrectPageAndTotal()
    {
        var (cat, participant) = await SeedAsync();

        await using var seedCtx = _db.NewContext();
        var trackedCat         = await seedCtx.Categories.FindAsync(cat.CategoryId);
        var trackedParticipant = await seedCtx.Participants.FindAsync(participant.ParticipantId);
        for (var i = 0; i < 5; i++)
            seedCtx.Ideas.Add(TestEntities.NewIdea(trackedCat, trackedParticipant));
        await seedCtx.SaveChangesAsync();

        await using var actCtx = _db.NewContext();
        var (items, total) = await new IdeaRepository(actCtx)
            .ReadIdeasPagedAsync(cat.Project.ProjectId, "", null, null, page: 2, pageSize: 2);

        Assert.Equal(5, total);
        Assert.Equal(2, items.Count);
    }
}
