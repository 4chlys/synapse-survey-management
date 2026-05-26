using SynapseEnqueteManagement.BL.AiIdeaSummary;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.BL.Tests.AiIdeaSummary;

public class IdeaSummarySamplingServiceTests
{
    private static List<Idea> MakeIdeas(int count) =>
        Enumerable.Range(1, count).Select(i => new Idea
        {
            IdeaId = i,
            Description = $"Idea {i}",
            CreatedAt = DateTime.UtcNow.AddMinutes(-i),
            Comments = Enumerable.Range(0, i % 5).Select(_ => new TextComment()).ToList<Comment>(),
        }).ToList();

    private static List<Idea> MakeIdeasWithEmbeddings(int count)
    {
        var ideas = MakeIdeas(count);
        for (int i = 0; i < ideas.Count; i++)
        {
            var emb = new float[4];
            emb[i % 4] = 1f;
            ideas[i].Embedding = emb;
        }
        return ideas;
    }

    [Fact]
    public void Sample_Newest_ReturnsCappedByNewest()
    {
        var svc = new IdeaSummarySamplingService();
        var ideas = MakeIdeas(10);

        var result = svc.Sample(ideas, IdeaSummarySamplingStrategy.Newest, cap: 3);

        Assert.Equal(3, result.Count);
        Assert.Equal(1, result[0].IdeaId); // newest first (smallest negative offset = IdeaId 1)
    }

    [Fact]
    public void Sample_MostCommented_ReturnsCappedByCommentCount()
    {
        var svc = new IdeaSummarySamplingService();
        var ideas = MakeIdeas(10);

        var result = svc.Sample(ideas, IdeaSummarySamplingStrategy.MostCommented, cap: 3);

        Assert.Equal(3, result.Count);
        Assert.True(result[0].Comments.Count >= result[1].Comments.Count);
    }

    [Fact]
    public void Sample_Diverse_WithEmbeddings_ReturnsCappedDiverseSet()
    {
        var svc = new IdeaSummarySamplingService();
        var ideas = MakeIdeasWithEmbeddings(8);

        var result = svc.Sample(ideas, IdeaSummarySamplingStrategy.Diverse, cap: 4);

        Assert.Equal(4, result.Count);
    }

    [Fact]
    public void Sample_Diverse_WithoutEmbeddings_FallsBackToNewest()
    {
        var svc = new IdeaSummarySamplingService();
        var ideas = MakeIdeas(10); // no embeddings

        var result = svc.Sample(ideas, IdeaSummarySamplingStrategy.Diverse, cap: 3);

        Assert.Equal(3, result.Count);
        Assert.Equal(1, result[0].IdeaId); // falls back to newest
    }

    [Fact]
    public void Sample_NullCap_ReturnsAll()
    {
        var svc = new IdeaSummarySamplingService();
        var ideas = MakeIdeas(5);

        var result = svc.Sample(ideas, IdeaSummarySamplingStrategy.Newest, cap: null);

        Assert.Equal(5, result.Count);
    }
}