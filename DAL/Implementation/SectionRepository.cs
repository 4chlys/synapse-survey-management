using SynapseEnqueteManagement.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.DAL.Implementation;

public class SectionRepository(DbContext context) : Repository<Section>(context), ISectionRepository
{
    public async Task<IEnumerable<Section>> ReadByProjectIdReadOnlyAsync(int projectId)
        => await Context.Set<Section>()
            .AsNoTracking()
            .Where(s => s.Questionnaire.Project.ProjectId == projectId)
            .ToListAsync();
}
