using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.DAL;

public interface IIdeaRepository : IRepository<Idea>
{
    Task<Idea> GetByIdWithCommentsAndParticipantReadOnlyAsync(int ideaId);

    Task<int> CountVisibleByCategoryAsync(int categoryId);

    Task<IEnumerable<Idea>> GetVisibleByCategoryWithCommentsReadOnlyAsync(
        int categoryId,
        Func<IQueryable<Idea>, IOrderedQueryable<Idea>> orderBy,
        bool randomize,
        int skip,
        int take);

    Task<IEnumerable<Idea>> GetByCategoryAndParticipantWithCommentsReadOnlyAsync(int categoryId, int participantId);
}
