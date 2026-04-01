using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Interfaces;
using SynapseEnqueteManagement.DAL;

namespace SynapseEnqueteManagement.BL.Implementation;

public class SectionManager(IRepository repository) : ISectionManager
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
            filters: [q => q.Project.ProjectId == projectId]
        );
        if (questionnaire is null) return null;

        var section = new Section
        {
            Name = name,
            Description = description,
            Questionnaire = questionnaire
        };
        return await repository.CreateAsync(section);
    }

    public async Task<bool> UpdateSectionAsync(int sectionId, string name, string
        description)
    {
        var section = await repository.ReadByIdAsync<Section>(sectionId);
        if (section is null) return false;

        section.Name = name;
        section.Description = description;
        return await repository.UpdateAsync(section);
    }

    public async Task<bool> DeleteSectionAsync(int sectionId)
    {
        return await repository.DeleteByIdAsync<Section>(sectionId);
    }
}