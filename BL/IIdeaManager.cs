using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.BL;

public interface IIdeaManager
{
    Task<IEnumerable<Idea>> GetIdeasByCategoryAsync(int categoryId);
    Task<Idea> AddIdeaAsync(int categoryId, int projectId, string description, ModerationStatus status, Participant participant);
    Task<Idea> GetIdeaByIdAsync(int ideaId);
    
}