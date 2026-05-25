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
}
