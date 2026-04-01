using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.BL.Interfaces;

public interface IProjectManager
{
    Task<Project> GetProjectByIdAsync(int projectId);
    Task<IEnumerable<Emoji>> GetAllEmojisAsync();
    Task<bool> EnableEmojiForProjectAsync(int projectId, int emojiId);
    Task<bool> DisableEmojiForProjectAsync(int projectId, int emojiId);
}