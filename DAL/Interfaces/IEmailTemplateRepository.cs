using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.DAL.Interfaces;

public interface IEmailTemplateRepository
{
    Task<EmailTemplate> GetByProjectIdAsync(int projectId);
    Task AddAsync(EmailTemplate template, CancellationToken ct = default);
    Task SaveAsync(CancellationToken ct = default);
}
