using System.Text.RegularExpressions;
using Resend;
using SynapseEnqueteManagement.BL.Interfaces;
using SynapseEnqueteManagement.BL.Models;

namespace SynapseEnqueteManagement.BL.Services;

public class ResendEmailService(IResend resend, string fromEmail) : IEmailService
{
    // Matches http(s) URLs; trailing punctuation that is typically sentence-terminating
    // is excluded so "Visit https://example.com." doesn't swallow the period.
    private static readonly Regex UrlRegex = new(@"https?://[^\s<]+[^\s<.,;:!?)\]]", RegexOptions.Compiled);

    public async Task SendAsync(EmailSendRequest request, CancellationToken ct = default)
    {
        var encoded = System.Net.WebUtility.HtmlEncode(request.Body);
        // Wrap URLs in <a> tags so clients that don't auto-link bare text (e.g. Outlook desktop) still render them clickable.
        // The encoded form is already valid inside an href (& → &amp;).
        var linked = UrlRegex.Replace(encoded, m => $"<a href=\"{m.Value}\">{m.Value}</a>");
        var htmlBody = "<div style=\"font-family: sans-serif;\">"
            + linked.Replace("\r\n", "<br>").Replace("\n", "<br>")
            + "</div>";
        var message = new EmailMessage
        {
            From = fromEmail,
            To = { request.To },
            Subject = request.Subject,
            HtmlBody = htmlBody
        };
        // Bcc / ReplyTo are nullable EmailAddressList on EmailMessage; the SDK
        // exposes implicit string[] / string conversions so we assign directly.
        if (request.Bcc != null && request.Bcc.Count > 0)
            message.Bcc = request.Bcc.ToArray();
        if (!string.IsNullOrWhiteSpace(request.ReplyTo))
            message.ReplyTo = request.ReplyTo;
        await resend.EmailSendAsync(message, ct);
    }
}
