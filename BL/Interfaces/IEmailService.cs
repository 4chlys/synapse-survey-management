using SynapseEnqueteManagement.BL.Models;

namespace SynapseEnqueteManagement.BL.Interfaces;

public interface IEmailService
{
    Task SendAsync(EmailSendRequest request, CancellationToken ct = default);
}
