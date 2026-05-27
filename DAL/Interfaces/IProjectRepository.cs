using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.DAL.Interfaces;

public interface IProjectRepository : IRepository<Project>
{
    Task<Project> ReadByIdReadOnlyAsync(int projectId);

    Task<Project> ReadByIdWithIntroductionReadOnlyAsync(int projectId);

    Task<Project> ReadByIdWithIntroductionForUpdateAsync(int projectId);

    Task<Project> ReadByIdWithEmojisForUpdateAsync(int projectId);

    Task<Project> ReadByIdWithEmojisReadOnlyAsync(int projectId);

    Task<Project> ReadByIdWithQuestionnaireForUpdateAsync(int projectId);
    
    Task<bool> ExistsByShortFormAsync(string shortForm, int? excludeProjectId = null);
    Task<Project> ReadByShortFormReadOnlyAsync(string shortForm);
    Task<IEnumerable<Project>> ReadAllWithPlatformReadOnlyAsync(int? platformId = null);

    Task<bool> DeleteWithRelationsAsync(int projectId);

    Task<Project> ReadByIdForDuplicationReadOnlyAsync(int projectId);

    Task<Project> ReadByIdWithTranslationsReadOnlyAsync(int projectId);

    Task<Project> ReadByIdWithTranslationsAndLanguagesForUpdateAsync(int projectId);

    Task<ProjectTranslation> ReadTranslationForUpdateAsync(int projectId, string languageCode);

    Task<Project> ReadByIdForActivationReadOnlyAsync(int projectId);
}
