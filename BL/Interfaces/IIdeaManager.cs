using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.BL.Interfaces;

public interface IIdeaManager
{
    Task<PagedResult<Idea>> GetVisibleIdeasByCategoryWithCommentsReadOnlyAsync(int categoryId, int pageSize, int page, string sortKey);
    Task<IEnumerable<Idea>> GetParticipantIdeasByCategoryWithCommentsReadOnlyAsync(int categoryId, int participantId);
    Task<Idea> GetIdeaByIdWithCommentsReadOnlyAsync(int ideaId);
    Task<AddIdeaResult> AddIdeaAsync(int categoryId, int projectId, string description, ModerationStatus status, Participant participant, CancellationToken ct = default);
    Task<Dictionary<int, int>> CountByCategoriesAsync(IEnumerable<int> categoryIds);
    Task<int> CountByCategoryAsync(int categoryId);
}
