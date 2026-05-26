using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.DAL.Tests.Integration;

public class CommentRepositoryTests : IDisposable
{
    private readonly InMemoryDatabase _db = new();

    public void Dispose() => _db.Dispose();

    private async Task<(TextComment Comment, int OtherProjectId)> SeedAsync()
    {
        await using var ctx = _db.NewContext();

        var lang         = TestEntities.NewLanguage();
        var project      = TestEntities.NewProject(lang);
        var otherProject = TestEntities.NewProject(lang);
        otherProject.ShortForm = "other-project";

        var category    = TestEntities.NewCategory(project);
        var participant = TestEntities.NewParticipant(project, lang);

        ctx.Languages.Add(lang);
        ctx.Projects.AddRange(project, otherProject);
        ctx.Categories.Add(category);
        ctx.Participants.Add(participant);
        await ctx.SaveChangesAsync();

        var idea = TestEntities.NewIdea(
            await ctx.Categories.FindAsync(category.CategoryId),
            await ctx.Participants.FindAsync(participant.ParticipantId));
        ctx.Ideas.Add(idea);
        await ctx.SaveChangesAsync();

        var comment = new TextComment
        {
            Idea             = idea,
            Participant      = await ctx.Participants.FindAsync(participant.ParticipantId),
            Text             = "hello",
            ModerationStatus = ModerationStatus.Unflagged,
            CreatedAt        = DateTime.UtcNow,
        };
        ctx.Comments.Add(comment);
        await ctx.SaveChangesAsync();

        return (comment, otherProject.ProjectId);
    }

    [Fact]
    public async Task ReadTextCommentByIdForUpdateAsync_CorrectProject_ReturnsTrackedComment()
    {
        var (seeded, _) = await SeedAsync();

        await using var actCtx = _db.NewContext();
        var result = await new CommentRepository(actCtx)
            .ReadTextCommentByIdForUpdateAsync(seeded.CommentId, seeded.Idea.Category.Project.ProjectId);

        Assert.NotNull(result);
        Assert.Equal(seeded.CommentId, result.CommentId);
    }

    [Fact]
    public async Task ReadTextCommentByIdForUpdateAsync_WrongProject_ReturnsNull()
    {
        var (seeded, otherProjectId) = await SeedAsync();

        await using var actCtx = _db.NewContext();
        var result = await new CommentRepository(actCtx)
            .ReadTextCommentByIdForUpdateAsync(seeded.CommentId, otherProjectId);

        Assert.Null(result);
    }
}
