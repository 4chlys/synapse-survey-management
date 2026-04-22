using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.BL.Sorting.Embeddings;

// Sorts ideas by cosine similarity to the query embedding, descending.
// Ideas without an embedding are placed last (sim = -∞) so they never crowd out
// genuinely similar results.
public class SimilarSortStrategy : IIdeaSortStrategy
{
    public string Key => "similar";

    public IReadOnlyList<Idea> Sort(IReadOnlyList<Idea> ideas, float[] queryEmbedding)
    {
        if (queryEmbedding == null || queryEmbedding.Length == 0)
            throw new ArgumentException("Query embedding is required for 'similar' sort", nameof(queryEmbedding));

        return ideas
            .Select(i => (idea: i, score: i.Embedding == null
                ? float.NegativeInfinity
                : VectorMath.CosineSimilarity(i.Embedding, queryEmbedding)))
            .OrderByDescending(x => x.score)
            .Select(x => x.idea)
            .ToList();
    }
}
