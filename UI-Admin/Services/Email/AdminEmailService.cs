using Resend;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.UI.Admin.Services.Email;

public class AdminEmailService(IResend resend, string fromEmail) : IEmailService
{
    public async Task SendAsync(string toEmail, string subject, string body, CancellationToken ct = default)
    {
        var htmlBody = "<div style=\"font-family: sans-serif;\">"
            + System.Net.WebUtility.HtmlEncode(body).Replace("\r\n", "<br>").Replace("\n", "<br>")
            + "</div>";
        var message = new EmailMessage { From = fromEmail, To = { toEmail }, Subject = subject, HtmlBody = htmlBody };
        await resend.EmailSendAsync(message, ct);
    }
}
