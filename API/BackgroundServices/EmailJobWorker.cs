using SynapseEnqueteManagement.BL.Interfaces;
using SynapseEnqueteManagement.BL.Models;

namespace SynapseEnqueteManagement.API.BackgroundServices;

// Polls the EmailJob queue every 5s, sends one row per tick. Mirrors
// IdeaSummaryWorker's PeriodicTimer / scope-per-tick shape; the extra
// retry/backoff logic lives in EmailJobManager.MarkFailedAsync.
public sealed class EmailJobWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<EmailJobWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval);
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try { await ProcessNextJobAsync(stoppingToken); }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { logger.LogError(ex, "[EmailJobWorker] Unhandled error in poll loop."); }
        }
    }

    private async Task ProcessNextJobAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var manager  = scope.ServiceProvider.GetRequiredService<IEmailJobManager>();
        var emailSvc = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var job = await manager.ClaimNextDueAsync(ct);
        if (job is null) return;

        logger.LogInformation(
            "[EmailJobWorker] Sending job {JobId} (attempt {Attempt}/{Max}, category {Category}).",
            job.EmailJobId, job.AttemptCount + 1, job.MaxAttempts, job.Category);

        try
        {
            var bcc = string.IsNullOrEmpty(job.BccAddressesCsv)
                ? Array.Empty<string>()
                : job.BccAddressesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries);

            await emailSvc.SendAsync(new EmailSendRequest(
                To:      job.ToAddress,
                Bcc:     bcc,
                ReplyTo: job.ReplyTo,
                Subject: job.Subject,
                Body:    job.Body), ct);

            await manager.MarkSentAsync(job, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex,
                "[EmailJobWorker] Job {JobId} send failed (attempt {Attempt}/{Max}).",
                job.EmailJobId, job.AttemptCount + 1, job.MaxAttempts);
            await manager.MarkFailedAsync(job, ex.Message, ct);
        }
    }
}
