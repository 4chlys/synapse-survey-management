using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Interfaces;
using SynapseEnqueteManagement.DAL.Interfaces;

namespace SynapseEnqueteManagement.BL.Implementation;

public class SectionManager(
    ISectionRepository sectionRepository,
    IQuestionnaireRepository questionnaireRepository,
    IUnitOfWork uow) : ISectionManager
{
    public Task<IEnumerable<Section>> GetSectionsAsync(int projectId)
        => sectionRepository.ReadByProjectIdReadOnlyAsync(projectId);

    public async Task<Section> CreateSectionAsync(int projectId, string name, string description)
    {
        var questionnaire = await questionnaireRepository.ReadByProjectIdForUpdateAsync(projectId);
        if (questionnaire is null) return null;

        var section = new Section
        {
            Name = name,
            Description = description,
            Questionnaire = questionnaire
        };
        var created = await sectionRepository.CreateAsync(section);
        await uow.SaveChangesAsync();
        return created;
    }

    public async Task<bool> UpdateSectionAsync(int sectionId, string name, string description)
    {
        var section = await sectionRepository.ReadByIdForUpdateAsync(sectionId);
        if (section is null) return false;

        section.Name = name;
        section.Description = description;
        sectionRepository.Update(section);
        await uow.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteSectionAsync(int sectionId)
    {
        var result = await sectionRepository.DeleteByIdAsync(sectionId);
        if (result) await uow.SaveChangesAsync();
        return result;
    }
}
