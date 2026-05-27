using Microsoft.Extensions.Logging;
using SynapseEnqueteManagement.BL.Interfaces;
using SynapseEnqueteManagement.BL.Models;

namespace SynapseEnqueteManagement.BL.Services;

public class LogEmailService(ILogger<LogEmailService> logger) : IEmailService
{
    public Task SendAsync(EmailSendRequest request, CancellationToken ct = default)
    {
        logger.LogInformation(
            "[DEV] Email to {To} (BCC {BccCount}) | Reply-To: {ReplyTo} | Subject: {Subject}",
            request.To, request.Bcc?.Count ?? 0, request.ReplyTo ?? "(none)", request.Subject);
        return Task.CompletedTask;
    }
}
