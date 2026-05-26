using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.BL.Interfaces;

public interface ISectionManager
{
    Task<IEnumerable<Section>> GetSectionsReadOnlyAsync(int projectId);
    Task<Section> CreateSectionAsync(int projectId, IEnumerable<SectionTranslationInput> translations, CancellationToken ct = default);
    Task<bool> UpdateSectionAsync(int sectionId, IEnumerable<SectionTranslationInput> translations, CancellationToken ct = default);
    Task<bool> DeleteSectionAsync(int sectionId, CancellationToken ct = default);
}
