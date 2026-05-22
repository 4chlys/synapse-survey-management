using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.API.Services;

public class LogEmailService(ILogger<LogEmailService> logger) : IEmailService
{
    public Task SendAsync(string toEmail, string subject, string body, CancellationToken ct = default)
    {
        logger.LogInformation("[DEV] Email to {To} | Subject: {Subject}", toEmail, subject);
        return Task.CompletedTask;
    }
}
