using Microsoft.EntityFrameworkCore;
using SynapseEnqueteManagement.BL.Domain.Platform.Entities;
using SynapseEnqueteManagement.DAL.Interfaces;

namespace SynapseEnqueteManagement.DAL.Implementation;

public class ProjectPlatformRepository(DbContext context)
    : Repository<ProjectPlatform>(context), IProjectPlatformRepository
{
    public async Task<IEnumerable<ProjectPlatform>> ReadAllOrderedByNameReadOnlyAsync()
        => await Context.Set<ProjectPlatform>()
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .ToListAsync();
}
