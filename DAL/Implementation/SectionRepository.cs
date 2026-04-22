using SynapseEnqueteManagement.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.DAL.Implementation;

public class SectionRepository(DbContext context) : Repository<Section>(context), ISectionRepository
{
    public async Task<IEnumerable<Section>> ReadByProjectIdReadOnlyAsync(int projectId)
        => await _context.Set<Section>()
            .AsNoTracking()
            .Where(s => s.Questionnaire.Project.ProjectId == projectId)
            .ToListAsync();

    public Task<Section> ReadByIdForUpdateAsync(int sectionId)
        => _context.Set<Section>()
            .FirstOrDefaultAsync(s => s.SectionId == sectionId);
}
