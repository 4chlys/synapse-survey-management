using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.DAL.Interfaces;

public interface IIdeaRepository : IRepository<Idea>
{
    Task<Idea> ReadByIdWithCommentsAndParticipantReadOnlyAsync(int ideaId);

    Task<Idea> ReadByIdWithProjectForUpdateAsync(int ideaId);

    Task<Idea> ReadByIdWithProjectAndEmojisForUpdateAsync(int ideaId);

    Task<int> CountVisibleByCategoryAsync(int categoryId);

    Task<int> CountByCategoryAsync(int categoryId);

    Task<Dictionary<int, int>> CountByCategoriesAsync(IEnumerable<int> categoryIds);

    Task<IEnumerable<Idea>> ReadVisibleByCategoryWithCommentsReadOnlyAsync(
        int categoryId,
        Func<IQueryable<Idea>, IOrderedQueryable<Idea>> orderBy,
        bool randomize,
        int skip,
        int take);

    Task<IEnumerable<Idea>> ReadByCategoryAndParticipantWithCommentsReadOnlyAsync(int categoryId, int participantId);

    Task<bool> AnyExistByParticipantAsync(int participantId);

    // Embedding-sorted reads. Implementation selects pgvector SQL (PostgreSQL) or
    // in-memory cosine sort (SQLite / other) based on the active provider.
    Task<IEnumerable<Idea>> ReadVisibleByCategoryOrderedByEmbeddingReadOnlyAsync(
        int categoryId, float[] queryEmbedding, bool ascending, int skip, int take);

    Task<IEnumerable<Idea>> ReadVisibleByCategoryTopNByEmbeddingReadOnlyAsync(
        int categoryId, float[] queryEmbedding, int maxItems);
}
