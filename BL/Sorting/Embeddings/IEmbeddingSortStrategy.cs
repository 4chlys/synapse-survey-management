using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.BL.Sorting.Embeddings;

// Embedding-based sort strategies differ from ISortStrategy<T> in two ways:
//   1) They cannot be expressed as a SQL ORDER BY — they require materialised
//      vectors and in-memory math.
//   2) They are parameterised by a query embedding (nullable for Diverse).
// Keeping them on their own abstraction avoids polluting ISortStrategy<T> with
// concerns that don't apply to its implementations.
public interface IEmbeddingSortStrategy
{
    string Key { get; }

    IReadOnlyList<Idea> Sort(IReadOnlyList<Idea> ideas, float[] queryEmbedding);
}
