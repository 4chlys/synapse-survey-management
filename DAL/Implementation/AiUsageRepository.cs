using Microsoft.EntityFrameworkCore;
using SynapseEnqueteManagement.BL.Domain.Ai.Entities;
using SynapseEnqueteManagement.DAL.Interfaces;

namespace SynapseEnqueteManagement.DAL.Implementation;

public class AiUsageRepository(DbContext context) : Repository<AiUsageEvent>(context), IAiUsageRepository
{
    public async Task LogAsync(AiUsageEvent ev, int projectId)
    {
        await Context.Set<AiUsageEvent>().AddAsync(ev);
        Context.Entry(ev).Property("ProjectId").CurrentValue = projectId;
        await Context.SaveChangesAsync();
    }

    public async Task<IEnumerable<ProjectCostAggregate>> ReadProjectAggregatesReadOnlyAsync(DateTime? from)
    {
        var query = Context.Set<AiUsageEvent>().AsNoTracking();
        if (from.HasValue) query = query.Where(e => e.CreatedAt >= from.Value);

        return await query
            .GroupBy(e => new
            {
                ProjectId = EF.Property<int>(e, "ProjectId"),
                e.Project.ShortForm,
                e.Project.ProjectPlatformId,
                PlatformName = e.Project.ProjectPlatform.Name
            })
            .Select(g => new ProjectCostAggregate(
                g.Key.ProjectId,
                g.Key.ShortForm,
                g.Key.ProjectPlatformId,
                g.Key.PlatformName,
                g.Sum(x => (long)x.InputTokens),
                g.Sum(x => (long)x.OutputTokens),
                g.Sum(x => x.RequestCount),
                g.Sum(x => x.CostEur)))
            .ToListAsync();
    }

    public async Task<IEnumerable<PlatformCostAggregate>> ReadPlatformAggregatesReadOnlyAsync(DateTime? from)
    {
        var query = Context.Set<AiUsageEvent>().AsNoTracking();
        if (from.HasValue) query = query.Where(e => e.CreatedAt >= from.Value);

        return await query
            .GroupBy(e => new
            {
                e.Project.ProjectPlatformId,
                PlatformName = e.Project.ProjectPlatform.Name
            })
            .Select(g => new PlatformCostAggregate(
                g.Key.ProjectPlatformId,
                g.Key.PlatformName,
                g.Sum(x => (long)x.InputTokens),
                g.Sum(x => (long)x.OutputTokens),
                g.Sum(x => x.RequestCount),
                g.Sum(x => x.CostEur)))
            .ToListAsync();
    }

    public async Task<IEnumerable<ServiceCostAggregate>> ReadServiceBreakdownByProjectReadOnlyAsync(int projectId, DateTime? from)
    {
        var query = Context.Set<AiUsageEvent>()
            .AsNoTracking()
            .Where(e => EF.Property<int>(e, "ProjectId") == projectId);
        if (from.HasValue) query = query.Where(e => e.CreatedAt >= from.Value);

        return await query
            .GroupBy(e => new { e.Service, e.Model })
            .Select(g => new ServiceCostAggregate(
                g.Key.Service,
                g.Key.Model,
                g.Sum(x => (long)x.InputTokens),
                g.Sum(x => (long)x.OutputTokens),
                g.Sum(x => x.RequestCount),
                g.Sum(x => x.CostEur)))
            .ToListAsync();
    }
}
