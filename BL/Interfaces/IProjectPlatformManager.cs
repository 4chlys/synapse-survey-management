using SynapseEnqueteManagement.BL.Domain.Platform.Entities;

namespace SynapseEnqueteManagement.BL.Interfaces;

public interface IProjectPlatformManager
{
    Task<IEnumerable<ProjectPlatform>> GetAllOrderedByNameReadOnlyAsync();
    Task<ProjectPlatform> GetByIdReadOnlyAsync(int id);
    Task<ProjectPlatform> CreateAsync(string name, string logoUrl, string websiteUrl);
    Task<bool> UpdateAsync(int id, string name, string logoUrl, string websiteUrl);
}
