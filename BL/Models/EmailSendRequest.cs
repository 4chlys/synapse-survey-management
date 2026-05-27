namespace SynapseEnqueteManagement.BL.Models;

// Input to IEmailService.SendAsync. To is the visible recipient; Bcc lets the
// queue worker fan out one admin-authored message to many participants in a
// single send. ReplyTo is the address recipients see when they click "Reply"
// — left null when the From address is the right reply target.
public sealed record EmailSendRequest(
    string To,
    IReadOnlyList<string> Bcc,
    string ReplyTo,
    string Subject,
    string Body)
{
    // Convenience for the synchronous endpoint and dev callers that don't
    // need BCC or Reply-To.
    public static EmailSendRequest Simple(string to, string subject, string body)
        => new(to, [], null, subject, body);
}
