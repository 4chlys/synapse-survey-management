using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.BL.Interfaces;

public interface IIdeaManager
{
    Task<PagedResult<Idea>> GetVisibleIdeasByCategoryWithCommentsAsync(int projectId, int categoryId, int page, string sortKey);
    Task<IEnumerable<Idea>> GetParticipantIdeasByCategoryWithCommentsAsync(int categoryId, int participantId);
    Task<AddIdeaResult> AddIdeaAsync(int categoryId, int projectId, string description, ModerationStatus status, Participant participant, CancellationToken ct = default);
    Task<Idea> GetIdeaByIdWithCommentsAsync(int ideaId);
}
