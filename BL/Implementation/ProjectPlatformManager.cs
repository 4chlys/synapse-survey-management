using SynapseEnqueteManagement.BL.Domain.Platform.Entities;
using SynapseEnqueteManagement.BL.Interfaces;
using SynapseEnqueteManagement.DAL.Interfaces;

namespace SynapseEnqueteManagement.BL.Implementation;

public class ProjectPlatformManager(
    IProjectPlatformRepository platformRepository,
    IUnitOfWork uow) : IProjectPlatformManager
{
    public Task<IEnumerable<ProjectPlatform>> GetAllOrderedByNameReadOnlyAsync()
        => platformRepository.ReadAllOrderedByNameReadOnlyAsync();

    public Task<ProjectPlatform> GetByIdReadOnlyAsync(int id)
        => platformRepository.ReadByIdForUpdateAsync(id);

    public async Task<ProjectPlatform> CreateAsync(string name, string logoUrl, string websiteUrl)
    {
        var platform = new ProjectPlatform
        {
            Name       = name,
            LogoUrl    = logoUrl,
            WebsiteUrl = websiteUrl,
            CreatedAt  = DateTime.UtcNow
        };
        await platformRepository.CreateAsync(platform);
        await uow.SaveChangesAsync();
        return platform;
    }

    public async Task<bool> UpdateAsync(int id, string name, string logoUrl, string websiteUrl)
    {
        var platform = await platformRepository.ReadByIdForUpdateAsync(id);
        if (platform is null) return false;

        platform.Name       = name;
        platform.LogoUrl    = logoUrl;
        platform.WebsiteUrl = websiteUrl;
        platformRepository.Update(platform);
        await uow.SaveChangesAsync();
        return true;
    }
}
