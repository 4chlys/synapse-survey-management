using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.DAL.Interfaces;

public interface IEmailTemplateRepository : IRepository<EmailTemplate>
{
    Task<EmailTemplate> ReadByProjectIdForUpdateAsync(int projectId);
    Task<EmailTemplate> CreateForProjectAsync(int projectId, CancellationToken ct = default);
}
