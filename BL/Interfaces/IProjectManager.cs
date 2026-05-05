using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.BL.Interfaces;

public record ProjectTranslationInput(string LanguageCode, string Name, string Topic);
public record IntroductionTranslationInput(string LanguageCode, string Text);

public interface IProjectManager
{
    Task<Project> GetProjectByIdWithIntroductionReadOnlyAsync(int projectId);
    Task<Project> GetProjectByIdReadOnlyAsync(int projectId);
    Task<Project> GetProjectByIdForUpdateAsync(int projectId);
    Task<Project> GetProjectByIdWithQuestionnaireForUpdateAsync(int projectId);
    Task<Project> GetProjectByIdWithTranslationsReadOnlyAsync(int projectId);
    Task<IEnumerable<Emoji>> GetEmojisForProjectAsync(int projectId);

    Task<Project> CreateProjectAsync(
        IEnumerable<ProjectTranslationInput> translations,
        IEnumerable<IntroductionTranslationInput> introductions,
        string defaultLanguageCode,
        IEnumerable<string> supportedLanguageCodes,
        string imageUrl, FormType formType, AiAssistanceType aiAssistance,
        int? minAge, int? maxAge, bool requestContactInfo, int platformId,
        string shortForm = null, int ideaPageSize = 5,
        bool extraIdeasReplaceExisting = false, int maxExtraIdeaRequests = 3);

    Task<Project> UpdateProjectAsync(int projectId,
        IEnumerable<ProjectTranslationInput> translations,
        IEnumerable<IntroductionTranslationInput> introductions,
        string defaultLanguageCode,
        IEnumerable<string> supportedLanguageCodes,
        string imageUrl, FormType formType, AiAssistanceType aiAssistance,
        int? minAge, int? maxAge, bool requestContactInfo,
        string shortForm = null, int ideaPageSize = 5,
        bool extraIdeasReplaceExisting = false, int maxExtraIdeaRequests = 3);

    Task<Project> GetProjectByShortFormAsync(string shortForm);
    Task<IEnumerable<Project>> GetAllProjectsAsync(int? platformId = null);
    Task IncrementViewCountAsync(int projectId);
    Task<IEnumerable<Emoji>> GetAllEmojisAsync();
    Task<bool> EnableEmojiForProjectAsync(int projectId, int emojiId, CancellationToken ct = default);
    Task<bool> DisableEmojiForProjectAsync(int projectId, int emojiId, CancellationToken ct = default);
    Task<Project> UpdateProjectStateAsync(int projectId, ProjectState newState);
    Task DeleteProjectAsync(int projectId);

    Task<Project> DuplicateProjectAsync(int sourceProjectId,
        IEnumerable<ProjectTranslationInput> translations,
        int platformId, string newShortForm = null, CancellationToken ct = default);
}
