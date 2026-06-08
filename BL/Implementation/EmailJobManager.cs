using System.Globalization;
using Microsoft.Extensions.Localization;
using SynapseEnqueteManagement.BL.Domain.Platform.Entities;
using SynapseEnqueteManagement.BL.Domain.Platform.Enums;
using SynapseEnqueteManagement.BL.Interfaces;
using SynapseEnqueteManagement.DAL.Interfaces;

namespace SynapseEnqueteManagement.BL.Implementation;

public sealed class EmailJobManager(
    IEmailJobRepository repo,
    IStringLocalizer<SharedResource> localizer) : IEmailJobManager
{
    // Backoff between retry attempts. Index = AttemptCount AFTER the failure
    // (so [0] is the wait before the 1st retry, [4] before the 5th retry).
    // Chosen to spread retries over ~8 hours and not hammer a flaky upstream.
    private static readonly TimeSpan[] BackoffSchedule =
    {
        TimeSpan.FromSeconds(30),
        TimeSpan.FromMinutes(2),
        TimeSpan.FromMinutes(10),
        TimeSpan.FromHours(1),
        TimeSpan.FromHours(6),
    };

    public async Task<EmailJob> EnqueueAsync(
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
        CancellationToken ct = default)
    {
        // BCC chunking is the caller's job: one row = one Resend send, capped
        // at ~50 recipients by the provider. The caller (controller/broadcast
        // service) splits big lists into multiple Enqueue calls.

        var finalBody = AppendReplyFooter(body, replyTo, languageCode);

        var job = new EmailJob
        {
            ToAddress       = to,
            BccAddressesCsv = bcc is { Count: > 0 } ? string.Join(",", bcc) : null,
            ReplyTo         = replyTo,
            Subject         = subject,
            Body            = finalBody,
            Category        = category,
            Status          = EmailJobStatus.Pending,
            LanguageCode    = languageCode,
            CreatedAt       = DateTime.UtcNow,
            ScheduledAt     = DateTime.UtcNow,
            CreatedByUserId = createdByUserId,
        };

        return await repo.CreateAsync(job, projectPlatformId, projectId, ideaId, ct);
    }

    public Task<EmailJob> ClaimNextDueAsync(CancellationToken ct = default)
        => repo.ClaimNextDueAsync(ct);

    public async Task MarkSentAsync(EmailJob job, CancellationToken ct = default)
    {
        job.Status = EmailJobStatus.Sent;
        job.SentAt = DateTime.UtcNow;
        job.LastError = null;
        await repo.UpdateAsync(job, ct);
    }

    public async Task MarkFailedAsync(EmailJob job, string errorMessage, CancellationToken ct = default)
    {
        job.AttemptCount += 1;
        job.LastError    = errorMessage;

        if (job.AttemptCount >= job.MaxAttempts)
        {
            // Out of attempts — stop retrying; operator page surfaces these.
            job.Status = EmailJobStatus.Failed;
        }
        else
        {
            // Schedule next attempt with backoff + ±20% jitter.
            var baseDelay = BackoffSchedule[Math.Min(job.AttemptCount - 1, BackoffSchedule.Length - 1)];
            var jitter    = 1.0 + (Random.Shared.NextDouble() * 0.4 - 0.2);
            job.Status      = EmailJobStatus.Pending;
            job.ScheduledAt = DateTime.UtcNow + TimeSpan.FromTicks((long)(baseDelay.Ticks * jitter));
        }

        await repo.UpdateAsync(job, ct);
    }

    public Task<List<EmailJob>> ListPendingAsync(int? projectPlatformId, int take, CancellationToken ct = default)
        => repo.ListByStatusReadOnlyAsync(EmailJobStatus.Pending, projectPlatformId, take, ct);

    public Task<List<EmailJob>> ListGivenUpAsync(int? projectPlatformId, int take, CancellationToken ct = default)
        => repo.ListByStatusReadOnlyAsync(EmailJobStatus.Failed, projectPlatformId, take, ct);

    public Task<List<EmailJob>> ListRecentSentAsync(int? projectPlatformId, int take, CancellationToken ct = default)
        => repo.ListRecentSentReadOnlyAsync(projectPlatformId, take, ct);

    public async Task<EmailJob> RetryAsync(int emailJobId, int? projectPlatformId = null, CancellationToken ct = default)
    {
        var job = await repo.ReadByIdForUpdateAsync(emailJobId, projectPlatformId, ct);
        if (job is null) return null;
        if (job.Status != EmailJobStatus.Failed) return job; // nothing to retry

        job.Status       = EmailJobStatus.Pending;
        job.AttemptCount = 0;
        job.ScheduledAt  = DateTime.UtcNow;
        job.LastError    = null;
        await repo.UpdateAsync(job, ct);
        return job;
    }

    // The localized footer is appended server-side so the canonical wording
    // is owned here, not in the admin's compose textarea (where they could
    // accidentally delete it). Skipped for categories that don't have a
    // participant-facing reply target (AdminInvite, PasswordReset).
    private string AppendReplyFooter(string body, string replyTo, string languageCode)
    {
        if (string.IsNullOrWhiteSpace(replyTo) || string.IsNullOrWhiteSpace(languageCode))
            return body;

        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(languageCode);
            var footer = localizer["Email_ReplyFooter", replyTo].Value;
            return $"{body}\n\n— {footer}";
        }
        catch (CultureNotFoundException)
        {
            // Unknown code — fall back to whatever the current culture is.
            var footer = localizer["Email_ReplyFooter", replyTo].Value;
            return $"{body}\n\n— {footer}";
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }
}
