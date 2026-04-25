using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.BL.Interfaces;

public interface IProjectManager
{
    Task<Project> GetProjectByIdWithIntroductionReadOnlyAsync(int projectId);
    Task<Project> GetProjectByIdReadOnlyAsync(int projectId);
    Task<Project> GetProjectByIdForUpdateAsync(int projectId);
    Task<IEnumerable<Emoji>> GetEmojisForProjectAsync(int projectId);
    Task<Project> CreateProjectAsync(string name, string topic, string introduction, string imageUrl, FormType formType, AiAssistanceType aiAssistance, int? minAge, int? maxAge, bool requestContactInfo);
    Task<Project> UpdateProjectAsync(int projectId, string name, string topic, string introduction, string imageUrl, FormType formType, AiAssistanceType aiAssistance, int? minAge, int? maxAge, bool requestContactInfo);
    Task<IEnumerable<Project>> GetAllProjectsAsync();
    Task<IEnumerable<Emoji>> GetAllEmojisAsync();
    Task<bool> EnableEmojiForProjectAsync(int projectId, int emojiId, CancellationToken ct = default);
    Task<bool> DisableEmojiForProjectAsync(int projectId, int emojiId, CancellationToken ct = default);
}
