using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.BL.Interfaces;

public interface IIdeaManager
{
    Task<PagedResult<Idea>> GetVisibleIdeasByCategoryWithCommentsAsync(int categoryId, int page, int pageSize, string sortKey);
    Task<IEnumerable<Idea>> GetParticipantIdeasByCategoryWithCommentsAsync(int categoryId, int participantId);
    Task<Idea> AddIdeaAsync(int categoryId, int projectId, string description, ModerationStatus status, Participant participant, CancellationToken ct = default);
    Task<Idea> GetIdeaByIdWithCommentsAsync(int ideaId);
    Task<int> GetIdeaCountByParticipantAsync(int participantId);
}
