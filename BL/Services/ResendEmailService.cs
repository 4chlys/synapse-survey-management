using System.Text.RegularExpressions;
using Resend;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.BL.Services;

public class ResendEmailService(IResend resend, string fromEmail) : IEmailService
{
    // Matches http(s) URLs; trailing punctuation that is typically sentence-terminating
    // is excluded so "Visit https://example.com." doesn't swallow the period.
    private static readonly Regex UrlRegex = new(@"https?://[^\s<]+[^\s<.,;:!?)\]]", RegexOptions.Compiled);

    public async Task SendAsync(string toEmail, string subject, string body, CancellationToken ct = default)
    {
        var encoded = System.Net.WebUtility.HtmlEncode(body);
        // Wrap URLs in <a> tags so clients that don't auto-link bare text (e.g. Outlook desktop) still render them clickable.
        // The encoded form is already valid inside an href (& → &amp;).
        var linked = UrlRegex.Replace(encoded, m => $"<a href=\"{m.Value}\">{m.Value}</a>");
        var htmlBody = "<div style=\"font-family: sans-serif;\">"
            + linked.Replace("\r\n", "<br>").Replace("\n", "<br>")
            + "</div>";
        var message = new EmailMessage { From = fromEmail, To = { toEmail }, Subject = subject, HtmlBody = htmlBody };
        await resend.EmailSendAsync(message, ct);
    }
}
