using Microsoft.Extensions.Localization;
using Moq;
using SynapseEnqueteManagement.BL;
using SynapseEnqueteManagement.BL.Domain.Platform.Entities;
using SynapseEnqueteManagement.BL.Domain.Platform.Enums;
using SynapseEnqueteManagement.BL.Implementation;
using SynapseEnqueteManagement.DAL.Interfaces;

namespace SynapseEnqueteManagement.API.Tests.Unit;

public class EmailJobManagerTests
{
    private readonly Mock<IEmailJobRepository> _repo = new();
    private readonly Mock<IStringLocalizer<SharedResource>> _localizer = new();

    public EmailJobManagerTests()
    {
        // Footer localizer: echo the reply address so tests can assert it was appended.
        _localizer
            .Setup(l => l[It.IsAny<string>(), It.IsAny<object[]>()])
            .Returns((string name, object[] args) =>
                new LocalizedString(name, $"Reply to {(args.Length > 0 ? args[0] : "")}"));
    }

    private EmailJobManager BuildManager() => new(_repo.Object, _localizer.Object);

    // CreateAsync echoes back the job it was handed, like the real repo does after SaveChanges.
    private void SetupCreateEchoes() =>
        _repo.Setup(r => r.CreateAsync(It.IsAny<EmailJob>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailJob j, int? _, int? _, int? _, CancellationToken _) => j);

    // ── EnqueueAsync ────────────────────────────────────────────────

    [Fact]
    public async Task EnqueueAsync_JoinsBccIntoCsv_AndStartsPendingDueNow()
    {
        SetupCreateEchoes();
        var before = DateTime.UtcNow;

        var job = await BuildManager().EnqueueAsync(
            to: "admin@x.com", bcc: new[] { "a@x.com", "b@x.com" }, replyTo: null,
            subject: "Hi", body: "Body", category: EmailJobCategory.AdminBroadcast,
            languageCode: null, projectPlatformId: 1, projectId: 2, ideaId: null,
            createdByUserId: "user-1");

        Assert.Equal("a@x.com,b@x.com", job.BccAddressesCsv);
        Assert.Equal(EmailJobStatus.Pending, job.Status);
        Assert.True(job.ScheduledAt >= before && job.ScheduledAt <= DateTime.UtcNow);
        Assert.Equal(0, job.AttemptCount);
    }

    [Fact]
    public async Task EnqueueAsync_AppendsLocalizedFooter_WhenReplyToAndLanguageGiven()
    {
        SetupCreateEchoes();

        var job = await BuildManager().EnqueueAsync(
            to: "admin@x.com", bcc: new[] { "p@x.com" }, replyTo: "reply@x.com",
            subject: "Hi", body: "Original body", category: EmailJobCategory.AdminReply,
            languageCode: "en", projectPlatformId: 1, projectId: 2, ideaId: 3,
            createdByUserId: "user-1");

        Assert.StartsWith("Original body", job.Body);
        Assert.Contains("Reply to reply@x.com", job.Body);
    }

    [Fact]
    public async Task EnqueueAsync_DoesNotAppendFooter_WhenNoReplyTo()
    {
        SetupCreateEchoes();

        var job = await BuildManager().EnqueueAsync(
            to: "x@x.com", bcc: System.Array.Empty<string>(), replyTo: null,
            subject: "S", body: "Just body", category: EmailJobCategory.AdminInvite,
            languageCode: null, projectPlatformId: 1, projectId: null, ideaId: null,
            createdByUserId: "u");

        Assert.Equal("Just body", job.Body);
        Assert.Null(job.BccAddressesCsv);
    }

    // ── MarkFailedAsync: backoff + give-up ──────────────────────────

    [Fact]
    public async Task MarkFailedAsync_BeforeMaxAttempts_StaysPendingAndSchedulesFuture()
    {
        var job = new EmailJob { Status = EmailJobStatus.Processing, AttemptCount = 0, MaxAttempts = 5 };

        await BuildManager().MarkFailedAsync(job, "smtp down");

        Assert.Equal(EmailJobStatus.Pending, job.Status);
        Assert.Equal(1, job.AttemptCount);
        Assert.Equal("smtp down", job.LastError);
        Assert.True(job.ScheduledAt > DateTime.UtcNow, "next attempt should be scheduled in the future (backoff)");
    }

    [Fact]
    public async Task MarkFailedAsync_AtMaxAttempts_FlipsToFailed()
    {
        var job = new EmailJob { Status = EmailJobStatus.Processing, AttemptCount = 4, MaxAttempts = 5 };

        await BuildManager().MarkFailedAsync(job, "still down");

        Assert.Equal(EmailJobStatus.Failed, job.Status);
        Assert.Equal(5, job.AttemptCount);
    }

    [Fact]
    public async Task MarkFailedAsync_BackoffGrows_AcrossAttempts()
    {
        var manager = BuildManager();
        var job = new EmailJob { Status = EmailJobStatus.Processing, AttemptCount = 0, MaxAttempts = 5 };

        await manager.MarkFailedAsync(job, "e");
        var firstDelay = job.ScheduledAt - DateTime.UtcNow;

        await manager.MarkFailedAsync(job, "e");
        var secondDelay = job.ScheduledAt - DateTime.UtcNow;

        // 30s (±20% jitter) then 2m (±20%) — the second window must clearly exceed the first.
        Assert.True(secondDelay > firstDelay,
            $"expected growing backoff, got {firstDelay} then {secondDelay}");
    }

    // ── MarkSentAsync ───────────────────────────────────────────────

    [Fact]
    public async Task MarkSentAsync_SetsSentAndClearsError()
    {
        var job = new EmailJob { Status = EmailJobStatus.Processing, LastError = "prev" };

        await BuildManager().MarkSentAsync(job);

        Assert.Equal(EmailJobStatus.Sent, job.Status);
        Assert.NotNull(job.SentAt);
        Assert.Null(job.LastError);
    }

    // ── RetryAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task RetryAsync_ResetsFailedJobToPending()
    {
        var job = new EmailJob
        {
            EmailJobId = 7, Status = EmailJobStatus.Failed,
            AttemptCount = 5, LastError = "gave up"
        };
        _repo.Setup(r => r.ReadByIdForUpdateAsync(7, It.IsAny<int?>(), It.IsAny<CancellationToken>())).ReturnsAsync(job);

        var result = await BuildManager().RetryAsync(7);

        Assert.Equal(EmailJobStatus.Pending, result.Status);
        Assert.Equal(0, result.AttemptCount);
        Assert.Null(result.LastError);
        _repo.Verify(r => r.UpdateAsync(job, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RetryAsync_DoesNothing_WhenJobNotFailed()
    {
        var job = new EmailJob { EmailJobId = 8, Status = EmailJobStatus.Pending, AttemptCount = 1 };
        _repo.Setup(r => r.ReadByIdForUpdateAsync(8, It.IsAny<int?>(), It.IsAny<CancellationToken>())).ReturnsAsync(job);

        var result = await BuildManager().RetryAsync(8);

        Assert.Equal(EmailJobStatus.Pending, result.Status);
        Assert.Equal(1, result.AttemptCount); // untouched
        _repo.Verify(r => r.UpdateAsync(It.IsAny<EmailJob>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RetryAsync_ReturnsNull_WhenJobMissing()
    {
        _repo.Setup(r => r.ReadByIdForUpdateAsync(It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<CancellationToken>())).ReturnsAsync((EmailJob)null);

        var result = await BuildManager().RetryAsync(99);

        Assert.Null(result);
    }

    [Fact]
    public async Task RetryAsync_ForwardsPlatformScopeToRepo()
    {
        // A PPA-scoped retry must reach the repo with the platform so the
        // ownership filter applies; a non-owned job comes back null → no retry.
        _repo.Setup(r => r.ReadByIdForUpdateAsync(7, 3, It.IsAny<CancellationToken>())).ReturnsAsync((EmailJob)null);

        var result = await BuildManager().RetryAsync(7, projectPlatformId: 3);

        Assert.Null(result);
        _repo.Verify(r => r.ReadByIdForUpdateAsync(7, 3, It.IsAny<CancellationToken>()), Times.Once);
    }
}
