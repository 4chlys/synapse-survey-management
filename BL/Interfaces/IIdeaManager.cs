using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.BL.Interfaces;

public interface IIdeaManager
{
    Task<PagedResult<Idea>> GetVisibleIdeasByCategoryWithCommentsReadOnlyAsync(int projectId, int categoryId, int page, string sortKey, int pageSize);
    Task<IEnumerable<Idea>> GetParticipantIdeasByCategoryWithCommentsReadOnlyAsync(int categoryId, int participantId);
    Task<AddIdeaResult> AddIdeaAsync(int categoryId, int projectId, string description, ModerationStatus status, Participant participant, CancellationToken ct = default);
    Task<Idea> GetIdeaByIdWithCommentsReadOnlyAsync(int ideaId);
    Task<IReadOnlyDictionary<int, int>> CountByCategoriesAsync(IEnumerable<int> categoryIds);
    Task<int> CountByCategoryAsync(int categoryId);
}
