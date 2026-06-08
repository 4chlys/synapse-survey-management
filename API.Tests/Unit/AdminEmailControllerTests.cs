using SynapseEnqueteManagement.API.DTOs.Email;
using SynapseEnqueteManagement.BL.Domain.Platform.Entities;
using SynapseEnqueteManagement.BL.Domain.Platform.Enums;

namespace SynapseEnqueteManagement.API.Tests.Unit;

public class AdminEmailControllerTests
{
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<IEmailJobManager> _jobManager = new();
    private readonly AdminEmailController _controller;

    public AdminEmailControllerTests()
    {
        _controller = new AdminEmailController(_emailService.Object, _jobManager.Object);
    }

    private static EnqueueEmailDto Dto(string category) => new(
        To: "admin@x.com", Bcc: new[] { "p@x.com" }, ReplyTo: "admin@x.com",
        Subject: "Hi", Body: "Body", Category: category, LanguageCode: "en",
        ProjectPlatformId: 1, ProjectId: 2, IdeaId: 3, CreatedByUserId: "user-1");

    [Fact]
    public async Task Enqueue_ParsesCategoryName_AndAcceptsWithJobId()
    {
        // Category travels as a string ("AdminReply"); the controller must parse it
        // to the enum — this is the path that previously 400'd.
        _jobManager
            .Setup(m => m.EnqueueAsync(
                It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), EmailJobCategory.AdminReply,
                It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int?>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailJob { EmailJobId = 42 });

        var result = await _controller.Enqueue(Dto("AdminReply"), CancellationToken.None);

        Assert.IsType<AcceptedResult>(result);
        _jobManager.VerifyAll();
    }

    [Fact]
    public async Task Enqueue_IsCaseInsensitiveOnCategory()
    {
        _jobManager
            .Setup(m => m.EnqueueAsync(
                It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), EmailJobCategory.AdminBroadcast,
                It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int?>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailJob { EmailJobId = 1 });

        var result = await _controller.Enqueue(Dto("adminbroadcast"), CancellationToken.None);

        Assert.IsType<AcceptedResult>(result);
    }

    [Fact]
    public async Task Enqueue_ReturnsBadRequest_OnUnknownCategory()
    {
        var result = await _controller.Enqueue(Dto("NotARealCategory"), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        _jobManager.Verify(m => m.EnqueueAsync(
            It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<EmailJobCategory>(),
            It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int?>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task List_RedactsRecipientAddressFromLastError()
    {
        var job = new EmailJob
        {
            EmailJobId = 1, ToAddress = "admin@x.com", Subject = "Hi",
            Category = EmailJobCategory.AdminReply, Status = EmailJobStatus.Failed,
            LastError = "550 rejected recipient <participant@example.com>"
        };
        _jobManager
            .Setup(m => m.ListGivenUpAsync(It.IsAny<int?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EmailJob> { job });

        var result = await _controller.List(EmailJobStatus.Failed, projectPlatformId: null, take: 100, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var row = Assert.Single(Assert.IsAssignableFrom<IEnumerable<EmailJobRowDto>>(ok.Value));
        Assert.DoesNotContain("participant@example.com", row.LastError);
        Assert.Contains("[redacted]", row.LastError);
    }

    [Theory]
    [InlineData(EmailJobCategory.AdminReply)]
    [InlineData(EmailJobCategory.AdminBroadcast)]
    public async Task List_MasksFreeTextSubject_ForUnscopedSystemAdmin(EmailJobCategory category)
    {
        var job = new EmailJob
        {
            EmailJobId = 1, ToAddress = "admin@x.com", Subject = "Re: your idea about the playground",
            Category = category, Status = EmailJobStatus.Failed
        };
        _jobManager
            .Setup(m => m.ListGivenUpAsync(It.IsAny<int?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EmailJob> { job });

        var result = await _controller.List(EmailJobStatus.Failed, projectPlatformId: null, take: 100, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var row = Assert.Single(Assert.IsAssignableFrom<IEnumerable<EmailJobRowDto>>(ok.Value));
        Assert.DoesNotContain("playground", row.Subject);
    }

    [Fact]
    public async Task List_KeepsFreeTextSubject_WhenScopedToOwningPlatform()
    {
        var job = new EmailJob
        {
            EmailJobId = 1, ToAddress = "admin@x.com", Subject = "Re: your idea about the playground",
            Category = EmailJobCategory.AdminReply, Status = EmailJobStatus.Failed
        };
        _jobManager
            .Setup(m => m.ListGivenUpAsync(It.IsAny<int?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EmailJob> { job });

        var result = await _controller.List(EmailJobStatus.Failed, projectPlatformId: 7, take: 100, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var row = Assert.Single(Assert.IsAssignableFrom<IEnumerable<EmailJobRowDto>>(ok.Value));
        Assert.Equal("Re: your idea about the playground", row.Subject);
    }

    [Fact]
    public async Task List_KeepsTemplatedSubject_EvenForUnscopedSystemAdmin()
    {
        var job = new EmailJob
        {
            EmailJobId = 1, ToAddress = "admin@x.com", Subject = "Welcome to MyPlatform",
            Category = EmailJobCategory.AdminInvite, Status = EmailJobStatus.Pending
        };
        _jobManager
            .Setup(m => m.ListPendingAsync(It.IsAny<int?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EmailJob> { job });

        var result = await _controller.List(EmailJobStatus.Pending, projectPlatformId: null, take: 100, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var row = Assert.Single(Assert.IsAssignableFrom<IEnumerable<EmailJobRowDto>>(ok.Value));
        Assert.Equal("Welcome to MyPlatform", row.Subject);
    }

    [Fact]
    public async Task Retry_ReturnsNotFound_WhenManagerReturnsNull()
    {
        _jobManager.Setup(m => m.RetryAsync(5, It.IsAny<int?>(), It.IsAny<CancellationToken>())).ReturnsAsync((EmailJob)null);

        var result = await _controller.Retry(5, projectPlatformId: null, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }
}
