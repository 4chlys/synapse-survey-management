using Microsoft.EntityFrameworkCore;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.DAL.Interfaces;

namespace SynapseEnqueteManagement.DAL.Implementation;

public class EmailTemplateRepository(DbContext context) : Repository<EmailTemplate>(context), IEmailTemplateRepository
{
    public Task<EmailTemplate> ReadByProjectIdForUpdateAsync(int projectId)
        => Context.Set<EmailTemplate>().FirstOrDefaultAsync(e => EF.Property<int>(e, "ProjectId") == projectId);

    public async Task<EmailTemplate> CreateForProjectAsync(int projectId, CancellationToken ct = default)
    {
        var template = new EmailTemplate();
        var entry = await Context.Set<EmailTemplate>().AddAsync(template, ct);
        entry.Property("ProjectId").CurrentValue = projectId;
        return template;
    }
}
