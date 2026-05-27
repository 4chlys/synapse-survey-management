using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SynapseEnqueteManagement.API.DTOs.Email;
using SynapseEnqueteManagement.BL.Domain.Platform.Entities;
using SynapseEnqueteManagement.BL.Domain.Platform.Enums;
using SynapseEnqueteManagement.BL.Interfaces;
using SynapseEnqueteManagement.BL.Models;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/admin/emails")]
[EnableRateLimiting("admin-email-send")]
public class AdminEmailController(
    IEmailService emailService,
    IEmailJobManager jobManager) : ControllerBase
{
    // Legacy synchronous path — kept for callers that haven't moved to the
    // queue yet (none after the AdminInvite/PasswordReset migration; remove
    // once verified). Production traffic should use Enqueue instead.
    [HttpPost]
    public async Task<ActionResult> Send(SendEmailDto dto, CancellationToken ct)
    {
        await emailService.SendAsync(EmailSendRequest.Simple(dto.To, dto.Subject, dto.Body), ct);
        return NoContent();
    }

    // Non-blocking path: persists one row to the EmailJob queue and returns
    // 202 immediately. The EmailJobWorker picks it up on its next 5s tick.
    [HttpPost("queue")]
    public async Task<ActionResult> Enqueue(EnqueueEmailDto dto, CancellationToken ct)
    {
        var job = await jobManager.EnqueueAsync(
            to:                 dto.To,
            bcc:                dto.Bcc ?? Array.Empty<string>(),
            replyTo:            dto.ReplyTo,
            subject:            dto.Subject,
            body:               dto.Body,
            category:           dto.Category,
            languageCode:       dto.LanguageCode,
            projectPlatformId:  dto.ProjectPlatformId,
            projectId:          dto.ProjectId,
            ideaId:             dto.IdeaId,
            createdByUserId:    dto.CreatedByUserId,
            ct:                 ct);

        return Accepted(new { emailJobId = job.EmailJobId });
    }

    // Operator-page reads. All three sections are paged via a single `take`
    // cap (defaults match what the page shows).
    [HttpGet("queue")]
    public async Task<ActionResult<IEnumerable<EmailJobRowDto>>> List(
        [FromQuery] EmailJobStatus status,
        [FromQuery] int? projectPlatformId,
        [FromQuery] int take,
        CancellationToken ct)
    {
        var capped = take is <= 0 or > 500 ? 100 : take;
        var rows = status switch
        {
            EmailJobStatus.Sent => await jobManager.ListRecentSentAsync(projectPlatformId, capped, ct),
            _                   => await jobManager.ListPendingAsync(projectPlatformId, capped, ct),
        };
        // Failed rows that have hit MaxAttempts ("given up") share status=Failed
        // with rows mid-backoff — caller passes status=Failed and filters
        // client-side if it only wants the given-up ones.
        if (status == EmailJobStatus.Failed)
            rows = await jobManager.ListGivenUpAsync(projectPlatformId, capped, ct);

        return Ok(rows.Select(Map));
    }

    [HttpPost("queue/{id:int}/retry")]
    public async Task<ActionResult<EmailJobRowDto>> Retry(int id, CancellationToken ct)
    {
        var job = await jobManager.RetryAsync(id, ct);
        if (job is null) return NotFound();
        return Ok(Map(job));
    }

    private static EmailJobRowDto Map(EmailJob j) => new(
        j.EmailJobId,
        j.ToAddress,
        j.Subject,
        j.Category,
        j.Status,
        j.LanguageCode,
        j.AttemptCount,
        j.MaxAttempts,
        j.CreatedAt,
        j.ScheduledAt,
        j.SentAt,
        j.LastError);
}
