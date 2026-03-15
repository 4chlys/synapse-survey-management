using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.BL.Implementation;

namespace SynapseEnqueteManagement.BL.Interfaces;

public interface IIdeaManager
{
    Task<PagedResult<Idea>> GetOkIdeasByCategoryAsync(int categoryId, int page, int pageSize, string sortKey, int? excludeParticipantId = null);
    Task<IEnumerable<Idea>> GetOwnIdeasByCategoryAsync(int categoryId, int participantId);
    Task<PagedResult<Idea>> GetIdeasWithOwnFirstAsync(int categoryId, int page, int pageSize, string sortKey, int? participantId);
    Task<Idea> AddIdeaAsync(int categoryId, int projectId, string description, ModerationStatus status, Participant participant);
    Task<Idea> GetIdeaByIdAsync(int ideaId);
}