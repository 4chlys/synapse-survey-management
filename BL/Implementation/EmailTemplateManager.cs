using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Interfaces;
using SynapseEnqueteManagement.DAL.Interfaces;

namespace SynapseEnqueteManagement.BL.Implementation;

public class EmailTemplateManager(IEmailTemplateRepository repository, IUnitOfWork uow) : IEmailTemplateManager
{
    public Task<EmailTemplate> GetByProjectIdAsync(int projectId)
        => repository.ReadByProjectIdForUpdateAsync(projectId);

    public async Task UpsertAsync(int projectId, string subject, string body, bool isEnabled, CancellationToken ct = default)
    {
        var template = await repository.ReadByProjectIdForUpdateAsync(projectId);
        if (template is null)
            template = await repository.CreateForProjectAsync(projectId, ct);

        template.Subject = subject;
        template.Body = body;
        template.IsEnabled = isEnabled;
        await uow.SaveChangesAsync(ct);
    }
}
