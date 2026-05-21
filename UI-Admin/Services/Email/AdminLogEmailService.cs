using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.UI.Admin.Services.Email;

public class AdminLogEmailService(ILogger<AdminLogEmailService> logger) : IEmailService
{
    public Task SendAsync(string toEmail, string subject, string body, CancellationToken ct = default)
    {
        logger.LogInformation("[Email-dev] To={To} Subject={Subject}", toEmail, subject);
        return Task.CompletedTask;
    }
}
