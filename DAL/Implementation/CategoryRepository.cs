using SynapseEnqueteManagement.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.DAL.Implementation;

public class CategoryRepository(DbContext context) : Repository<Category>(context), ICategoryRepository
{
    public async Task<IEnumerable<Category>> ReadByProjectIdReadOnlyAsync(int projectId)
        => await _context.Set<Category>()
            .AsNoTracking()
            .Where(c => c.Project.ProjectId == projectId)
            .ToListAsync();

    public Task<Category> ReadByIdReadOnlyAsync(int categoryId)
        => _context.Set<Category>()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CategoryId == categoryId);

    public Task<Category> ReadByIdForUpdateAsync(int categoryId)
        => _context.Set<Category>()
            .FirstOrDefaultAsync(c => c.CategoryId == categoryId);

    public Task<Category> ReadByIdWithProjectForUpdateAsync(int categoryId)
        => _context.Set<Category>()
            .Include(c => c.Project)
            .FirstOrDefaultAsync(c => c.CategoryId == categoryId);
}
