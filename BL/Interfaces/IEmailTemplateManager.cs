using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.BL.Interfaces;

public interface IEmailTemplateManager
{
    Task<EmailTemplate> GetByProjectIdAsync(int projectId);
    Task UpsertAsync(int projectId, string subject, string body, bool isEnabled, CancellationToken ct = default);
}
