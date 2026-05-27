using SynapseEnqueteManagement.BL.Domain.Platform.Entities;
using SynapseEnqueteManagement.BL.Domain.Platform.Enums;

namespace SynapseEnqueteManagement.BL.Interfaces;

public interface IEmailJobManager
{
    // Enqueues one row. Foreign-key ids are scalar inputs (the DAL plumbs
    // them into shadow FK columns). For localized categories the manager
    // appends the localized "Reply please to {0}" footer to body — admins
    // can't accidentally delete it from the modal.
    Task<EmailJob> EnqueueAsync(
        string to,
        IReadOnlyList<string> bcc,
        string replyTo,
        string subject,
        string body,
        EmailJobCategory category,
        string languageCode,
        int? projectPlatformId,
        int? projectId,
        int? ideaId,
        string createdByUserId,
        CancellationToken ct = default);

    Task<EmailJob> ClaimNextDueAsync(CancellationToken ct = default);
    Task MarkSentAsync(EmailJob job, CancellationToken ct = default);
    Task MarkFailedAsync(EmailJob job, string errorMessage, CancellationToken ct = default);

    Task<List<EmailJob>> ListPendingAsync(int? projectPlatformId, int take, CancellationToken ct = default);
    Task<List<EmailJob>> ListGivenUpAsync(int? projectPlatformId, int take, CancellationToken ct = default);
    Task<List<EmailJob>> ListRecentSentAsync(int? projectPlatformId, int take, CancellationToken ct = default);

    // Operator action: bump Failed (max-attempts-exhausted) rows back to
    // Pending with AttemptCount reset, due immediately.
    Task<EmailJob> RetryAsync(int emailJobId, CancellationToken ct = default);
}
