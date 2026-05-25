using SynapseEnqueteManagement.BL.Domain.I18n.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.BL.Interfaces;

public interface IProjectDuplicationService
{
    // Builds an unsaved deep copy of src — translations, introduction, the full questionnaire
    // graph (sections, questions, ending answers, parent links), categories and emoji links.
    // Validation, short-form resolution and persistence stay with the caller (ProjectManager).
    Task<Project> BuildCloneAsync(
        Project src,
        IReadOnlyList<ProjectTranslationInput> translations,
        IReadOnlyDictionary<string, Language> languages,
        string shortForm,
        int platformId);
}
