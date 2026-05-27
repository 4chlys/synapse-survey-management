using SynapseEnqueteManagement.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.DAL.Implementation;

public class SectionRepository(DbContext context) : Repository<Section>(context), ISectionRepository
{
    public async Task<IEnumerable<Section>> ReadByProjectIdWithTranslationsReadOnlyAsync(int projectId)
        => await Context.Set<Section>()
            .AsNoTracking()
            .Include(s => s.Translations).ThenInclude(t => t.Language)
            .Where(s => s.Questionnaire.Project.ProjectId == projectId)
            .ToListAsync();
}
