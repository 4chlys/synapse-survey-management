using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.BL.Interfaces;

public interface ISectionManager
{
    Task<IEnumerable<Section>> GetSectionsAsync(int projectId);
    Task<Section> CreateSectionAsync(int projectId, string name, string
        description);
    Task<bool> UpdateSectionAsync(int sectionId, string name, string
        description);
    Task<bool> DeleteSectionAsync(int sectionId);
}