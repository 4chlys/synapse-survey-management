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
    public async Task Retry_ReturnsNotFound_WhenManagerReturnsNull()
    {
        _jobManager.Setup(m => m.RetryAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((EmailJob)null);

        var result = await _controller.Retry(5, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }
}
