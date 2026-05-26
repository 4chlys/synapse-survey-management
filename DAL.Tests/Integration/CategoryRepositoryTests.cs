using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.DAL.Tests.Integration;

public class CategoryRepositoryTests : IDisposable
{
    private readonly InMemoryDatabase _db = new();

    public void Dispose() => _db.Dispose();

    private async Task<(Category Cat1, Category Cat2, Participant Participant)> SeedTwoCategoriesAsync()
    {
        await using var ctx = _db.NewContext();

        var lang        = TestEntities.NewLanguage();
        var project     = TestEntities.NewProject(lang);
        var cat1        = TestEntities.NewCategory(project);
        var cat2        = TestEntities.NewCategory(project);
        var participant = TestEntities.NewParticipant(project, lang);

        ctx.Languages.Add(lang);
        ctx.Projects.Add(project);
        ctx.Categories.AddRange(cat1, cat2);
        ctx.Participants.Add(participant);
        await ctx.SaveChangesAsync();

        return (cat1, cat2, participant);
    }

    [Fact]
    public async Task ReadModerationQueueAsync_WithoutCategoryFilter_CountsAllIdeasInProject()
    {
        var (cat1, cat2, participant) = await SeedTwoCategoriesAsync();

        await using var seedCtx = _db.NewContext();
        var trackedCat1         = await seedCtx.Categories.FindAsync(cat1.CategoryId);
        var trackedCat2         = await seedCtx.Categories.FindAsync(cat2.CategoryId);
        var trackedParticipant  = await seedCtx.Participants.FindAsync(participant.ParticipantId);
        seedCtx.Ideas.Add(TestEntities.NewIdea(trackedCat1, trackedParticipant, ModerationStatus.Approved));
        seedCtx.Ideas.Add(TestEntities.NewIdea(trackedCat2, trackedParticipant, ModerationStatus.Flagged));
        await seedCtx.SaveChangesAsync();

        await using var actCtx = _db.NewContext();
        var result = await new CategoryRepository(actCtx)
            .ReadModerationQueueAsync(cat1.Project.ProjectId);

        Assert.Equal(1, result.GetValueOrDefault(ModerationStatus.Approved));
        Assert.Equal(1, result.GetValueOrDefault(ModerationStatus.Flagged));
    }

    [Fact]
    public async Task ReadModerationQueueAsync_WithCategoryFilter_CountsOnlyThatCategory()
    {
        var (cat1, cat2, participant) = await SeedTwoCategoriesAsync();

        await using var seedCtx = _db.NewContext();
        var trackedCat1         = await seedCtx.Categories.FindAsync(cat1.CategoryId);
        var trackedCat2         = await seedCtx.Categories.FindAsync(cat2.CategoryId);
        var trackedParticipant  = await seedCtx.Participants.FindAsync(participant.ParticipantId);
        seedCtx.Ideas.Add(TestEntities.NewIdea(trackedCat1, trackedParticipant, ModerationStatus.Approved));
        seedCtx.Ideas.Add(TestEntities.NewIdea(trackedCat2, trackedParticipant, ModerationStatus.Approved));
        seedCtx.Ideas.Add(TestEntities.NewIdea(trackedCat2, trackedParticipant, ModerationStatus.Flagged));
        await seedCtx.SaveChangesAsync();

        await using var actCtx = _db.NewContext();
        var result = await new CategoryRepository(actCtx)
            .ReadModerationQueueAsync(cat1.Project.ProjectId, categoryId: cat2.CategoryId);

        Assert.Equal(1, result.GetValueOrDefault(ModerationStatus.Approved));
        Assert.Equal(1, result.GetValueOrDefault(ModerationStatus.Flagged));
        Assert.Equal(0, result.GetValueOrDefault(ModerationStatus.Unflagged));
    }
}
