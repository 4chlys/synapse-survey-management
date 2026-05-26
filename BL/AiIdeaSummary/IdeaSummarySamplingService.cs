using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.BL.Sorting.Embeddings;

namespace SynapseEnqueteManagement.BL.AiIdeaSummary;

public interface IIdeaSummarySamplingService
{
    List<Idea> Sample(IReadOnlyList<Idea> ideas, IdeaSummarySamplingStrategy strategy, int? cap);
}

public sealed class IdeaSummarySamplingService : IIdeaSummarySamplingService
{
    public List<Idea> Sample(
        IReadOnlyList<Idea> ideas,
        IdeaSummarySamplingStrategy strategy,
        int? cap)
    {
        var ordered = strategy switch
        {
            IdeaSummarySamplingStrategy.MostCommented =>
                ideas.OrderByDescending(i => i.Comments.Count).AsEnumerable(),

            IdeaSummarySamplingStrategy.Diverse =>
                SampleDiverse(ideas),

            _ => // Newest (default)
                ideas.OrderByDescending(i => i.CreatedAt).AsEnumerable(),
        };

        return cap.HasValue ? ordered.Take(cap.Value).ToList() : ordered.ToList();
    }

    private static IEnumerable<Idea> SampleDiverse(IReadOnlyList<Idea> ideas)
    {
        bool anyEmbeddings = ideas.Any(i => i.Embedding is { Length: > 0 });
        if (!anyEmbeddings)
            return ideas.OrderByDescending(i => i.CreatedAt); // fallback to newest

        // Reuse the existing MMR-based DiverseSortStrategy (centroid path, no query embedding)
        var strategy = new DiverseSortStrategy();
        return strategy.Sort(ideas, queryEmbedding: null);
    }
}