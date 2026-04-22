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
}
