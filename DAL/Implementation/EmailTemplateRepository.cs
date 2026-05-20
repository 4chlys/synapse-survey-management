using Microsoft.EntityFrameworkCore;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.DAL.Interfaces;

namespace SynapseEnqueteManagement.DAL.Implementation;

public class EmailTemplateRepository(SynapseEnqueteContext context) : IEmailTemplateRepository
{
    public Task<EmailTemplate> GetByProjectIdAsync(int projectId)
        => context.EmailTemplates.FirstOrDefaultAsync(e => e.ProjectId == projectId);

    public async Task AddAsync(EmailTemplate template, CancellationToken ct = default)
        => await context.EmailTemplates.AddAsync(template, ct);

    public Task SaveAsync(CancellationToken ct = default)
        => context.SaveChangesAsync(ct);
}
