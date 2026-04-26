using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.BL.Sorting.Embeddings;

// Inverse of SimilarSortStrategy — cosine similarity ascending, i.e. most
// *unlike* the query first. Ideas without an embedding still go last (+∞)
// because "we don't know how different they are" is not a reason to surface them.
public class DifferentSortStrategy : IEmbeddingSortStrategy
{
    public string Key => "different";

    public IReadOnlyList<Idea> Sort(IReadOnlyList<Idea> ideas, float[] queryEmbedding)
    {
        if (queryEmbedding == null || queryEmbedding.Length == 0)
            throw new ArgumentException("Query embedding is required for 'different' sort", nameof(queryEmbedding));

        return ideas
            .Select(i => (idea: i, score: i.Embedding == null
                ? float.PositiveInfinity
                : VectorMath.CosineSimilarity(i.Embedding, queryEmbedding)))
            .OrderBy(x => x.score)
            .Select(x => x.idea)
            .ToList();
    }
}
