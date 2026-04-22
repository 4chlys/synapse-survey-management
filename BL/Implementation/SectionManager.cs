using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Interfaces;
using SynapseEnqueteManagement.DAL;

namespace SynapseEnqueteManagement.BL.Implementation;

public class SectionManager(IRepository repository, IUnitOfWork uow) : ISectionManager
{
    public async Task<IEnumerable<Section>> GetSectionsAsync(int projectId)
    {
        return await repository.ReadAsync<Section>(
            filters: [s => s.Questionnaire.Project.ProjectId == projectId]
        );
    }

    public async Task<Section> CreateSectionAsync(int projectId, string name,
        string description)
    {
        var questionnaire = await repository.ReadSingleAsync<Questionnaire>(
            filters: [q => q.Project.ProjectId == projectId],
            tracked: true
        );
        if (questionnaire is null) return null;

        var section = new Section
        {
            Name = name,
            Description = description,
            Questionnaire = questionnaire
        };
        var created = await repository.CreateAsync(section);
        await uow.SaveChangesAsync();
        return created;
    }

    public async Task<bool> UpdateSectionAsync(int sectionId, string name, string
        description)
    {
        var section = await repository.ReadByIdAsync<Section>(sectionId);
        if (section is null) return false;

        section.Name = name;
        section.Description = description;
        repository.Update(section);
        await uow.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteSectionAsync(int sectionId)
    {
        var result = await repository.DeleteByIdAsync<Section>(sectionId);
        if (result) await uow.SaveChangesAsync();
        return result;
    }
}