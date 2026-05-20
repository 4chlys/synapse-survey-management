using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Interfaces;
using SynapseEnqueteManagement.DAL.Interfaces;

namespace SynapseEnqueteManagement.BL.Implementation;

public class EmailTemplateManager(IEmailTemplateRepository repository) : IEmailTemplateManager
{
    public Task<EmailTemplate> GetByProjectIdAsync(int projectId)
        => repository.GetByProjectIdAsync(projectId);

    public async Task UpsertAsync(int projectId, string subject, string body, bool isEnabled, CancellationToken ct = default)
    {
        var template = await repository.GetByProjectIdAsync(projectId);
        if (template is null)
        {
            template = new EmailTemplate { ProjectId = projectId };
            await repository.AddAsync(template, ct);
        }
        template.Subject = subject;
        template.Body = body;
        template.IsEnabled = isEnabled;
        await repository.SaveAsync(ct);
    }
}
