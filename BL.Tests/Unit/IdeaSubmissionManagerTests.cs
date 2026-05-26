using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using SynapseEnqueteManagement.BL;
using SynapseEnqueteManagement.BL.Ai;
using SynapseEnqueteManagement.BL.AiCommentSupport;
using SynapseEnqueteManagement.BL.AiIdeaSupport;
using SynapseEnqueteManagement.BL.AiModeration;
using SynapseEnqueteManagement.BL.AiQuestionSupport;
using SynapseEnqueteManagement.BL.Exceptions;

namespace SynapseEnqueteManagement.BL.Tests.Unit;

public class IdeaSubmissionManagerTests
{
    private readonly Mock<IModerationResolver> _moderationResolver = new(MockBehavior.Strict);
    private readonly Mock<IModerationTokenService> _moderationTokenService = new(MockBehavior.Strict);
    private readonly Mock<IIdeaManager> _ideaManager = new(MockBehavior.Strict);
    private readonly IdeaSubmissionManager _manager;

    private const int CategoryId = 10;
    private const int ProjectId = 100;
    private readonly Participant _participant = new() { ParticipantId = 1 };
    private readonly Project _project;
    private readonly ResolvedPlatformAiConfig _platformConfig;

    // Encodes the invariant that promotion must not happen unless it is reached.
    private static readonly Func<string, Task<string>> PromoteNotCalled =
        _ => throw new InvalidOperationException("promoteImageAsync must not be called");

    private static Func<string, Task<string>> PromoteTo(string permanentUrl) =>
        _ => Task.FromResult(permanentUrl);

    public IdeaSubmissionManagerTests()
    {
        _project = new Project { ProjectId = ProjectId };
        _platformConfig = new ResolvedPlatformAiConfig(
            Mock.Of<IChatClient>(),
            "gemini-2.0-flash",
            new ModerationConfig(),
            new AiIdeaSupportConfig(),
            new AiCommentSupportConfig(),
            new AiQuestionSupportConfig());
        _manager = new IdeaSubmissionManager(
            _moderationResolver.Object,
            _moderationTokenService.Object,
            _ideaManager.Object,
            NullLogger<IdeaSubmissionManager>.Instance);
    }

    [Fact]
    public async Task AddModeratedIdeaAsync_HappyPath_DelegatesResolvedStatusToIdeaManager()
    {
        // Arrange
        _moderationResolver.Setup(m => m.ResolveStatusAsync("desc", "tok", ProjectId, _platformConfig, It.IsAny<CancellationToken>(), "en"))
            .ReturnsAsync(ModerationStatus.Approved)
            .Verifiable(Times.Exactly(1));
        _ideaManager.Setup(i => i.AddIdeaAsync(CategoryId, ProjectId, "desc", ModerationStatus.Approved, _participant, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AddIdeaResult(42, false))
            .Verifiable(Times.Exactly(1));

        // Act
        var result = await _manager.AddModeratedIdeaAsync(_project, CategoryId, "desc", "tok",
            tempImageUrl: null, imageModerationToken: null, languageCode: null,
            _participant, _platformConfig, PromoteNotCalled, respondInLanguage: "en");

        // Assert
        Assert.Equal(42, result.IdeaId);
        _moderationResolver.VerifyAll();
        _ideaManager.VerifyAll();
    }

    [Fact]
    public async Task AddModeratedIdeaAsync_InvalidImageToken_ThrowsLocalizedValidationException_AndDoesNotCallIdeaManager()
    {
        // Arrange
        _moderationResolver.Setup(m => m.ResolveStatusAsync(It.IsAny<string>(), It.IsAny<string>(), ProjectId, _platformConfig, It.IsAny<CancellationToken>(), It.IsAny<string>()))
            .ReturnsAsync(ModerationStatus.Approved);
        _moderationTokenService.Setup(s => s.ValidateImageToken("bad-token", ProjectId))
            .Returns((ModerationStatus?)null);

        // Act + Assert
        var ex = await Assert.ThrowsAsync<LocalizedValidationException>(() =>
            _manager.AddModeratedIdeaAsync(_project, CategoryId, "desc", null,
                tempImageUrl: "tmp.png", imageModerationToken: "bad-token", languageCode: null,
                _participant, _platformConfig, PromoteNotCalled));

        Assert.Equal("Validation.Image.InvalidToken", ex.Code);
        _ideaManager.Verify(i => i.AddIdeaAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(),
            It.IsAny<ModerationStatus>(), It.IsAny<Participant>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddModeratedIdeaAsync_MissingImageToken_ThrowsLocalizedValidationException()
    {
        // Arrange — image present but no token supplied at all
        _moderationResolver.Setup(m => m.ResolveStatusAsync(It.IsAny<string>(), It.IsAny<string>(), ProjectId, _platformConfig, It.IsAny<CancellationToken>(), It.IsAny<string>()))
            .ReturnsAsync(ModerationStatus.Approved);

        // Act + Assert
        var ex = await Assert.ThrowsAsync<LocalizedValidationException>(() =>
            _manager.AddModeratedIdeaAsync(_project, CategoryId, "desc", null,
                tempImageUrl: "tmp.png", imageModerationToken: null, languageCode: null,
                _participant, _platformConfig, PromoteNotCalled));

        Assert.Equal("Validation.Image.InvalidToken", ex.Code);
    }

    [Fact]
    public async Task AddModeratedIdeaAsync_FlaggedImage_OverridesTextStatusToFlagged()
    {
        // Arrange — text resolves to Approved but the image was flagged by the AI; user chose to submit anyway
        _moderationResolver.Setup(m => m.ResolveStatusAsync(It.IsAny<string>(), It.IsAny<string>(), ProjectId, _platformConfig, It.IsAny<CancellationToken>(), It.IsAny<string>()))
            .ReturnsAsync(ModerationStatus.Approved);
        _moderationTokenService.Setup(s => s.ValidateImageToken("img-tok", ProjectId))
            .Returns(ModerationStatus.Flagged);
        _ideaManager.Setup(i => i.AddIdeaAsync(CategoryId, ProjectId, "desc", ModerationStatus.Flagged, _participant, "permanent.png", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AddIdeaResult(11, false))
            .Verifiable(Times.Exactly(1));

        // Act
        await _manager.AddModeratedIdeaAsync(_project, CategoryId, "desc", null,
            tempImageUrl: "tmp.png", imageModerationToken: "img-tok", languageCode: null,
            _participant, _platformConfig, PromoteTo("permanent.png"));

        // Assert
        _ideaManager.VerifyAll();
    }

    [Fact]
    public async Task AddModeratedIdeaAsync_ApprovedImage_KeepsResolvedTextStatus()
    {
        // Arrange
        _moderationResolver.Setup(m => m.ResolveStatusAsync(It.IsAny<string>(), It.IsAny<string>(), ProjectId, _platformConfig, It.IsAny<CancellationToken>(), It.IsAny<string>()))
            .ReturnsAsync(ModerationStatus.Approved);
        _moderationTokenService.Setup(s => s.ValidateImageToken("img-tok", ProjectId))
            .Returns(ModerationStatus.Approved);
        _ideaManager.Setup(i => i.AddIdeaAsync(CategoryId, ProjectId, "desc", ModerationStatus.Approved, _participant, "permanent.png", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AddIdeaResult(12, false))
            .Verifiable(Times.Exactly(1));

        // Act
        await _manager.AddModeratedIdeaAsync(_project, CategoryId, "desc", null,
            tempImageUrl: "tmp.png", imageModerationToken: "img-tok", languageCode: null,
            _participant, _platformConfig, PromoteTo("permanent.png"));

        // Assert
        _ideaManager.VerifyAll();
    }

    [Fact]
    public async Task AddModeratedIdeaAsync_CrossLanguageAllowed_AndCodeSupported_ForwardsCode()
    {
        // Arrange
        _project.AllowCrossLanguageIdeas = true;
        _project.SupportedLanguages = new List<Language>
        {
            new() { Code = "en" },
            new() { Code = "fr" }
        };
        _moderationResolver.Setup(m => m.ResolveStatusAsync(It.IsAny<string>(), It.IsAny<string>(), ProjectId, _platformConfig, It.IsAny<CancellationToken>(), It.IsAny<string>()))
            .ReturnsAsync(ModerationStatus.Approved);
        _ideaManager.Setup(i => i.AddIdeaAsync(CategoryId, ProjectId, "desc", ModerationStatus.Approved, _participant, null, "fr", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AddIdeaResult(13, false))
            .Verifiable(Times.Exactly(1));

        // Act
        await _manager.AddModeratedIdeaAsync(_project, CategoryId, "desc", null,
            tempImageUrl: null, imageModerationToken: null, languageCode: "fr",
            _participant, _platformConfig, PromoteNotCalled);

        // Assert
        _ideaManager.VerifyAll();
    }

    [Fact]
    public async Task AddModeratedIdeaAsync_CrossLanguageAllowed_ButCodeUnsupported_Throws()
    {
        // Arrange
        _project.AllowCrossLanguageIdeas = true;
        _project.SupportedLanguages = new List<Language> { new() { Code = "en" } };
        _moderationResolver.Setup(m => m.ResolveStatusAsync(It.IsAny<string>(), It.IsAny<string>(), ProjectId, _platformConfig, It.IsAny<CancellationToken>(), It.IsAny<string>()))
            .ReturnsAsync(ModerationStatus.Approved);

        // Act + Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _manager.AddModeratedIdeaAsync(_project, CategoryId, "desc", null,
                tempImageUrl: null, imageModerationToken: null, languageCode: "fr",
                _participant, _platformConfig, PromoteNotCalled));

        _ideaManager.Verify(i => i.AddIdeaAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(),
            It.IsAny<ModerationStatus>(), It.IsAny<Participant>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddModeratedIdeaAsync_CrossLanguageDisabled_IgnoresLanguageCode()
    {
        // Arrange — when project doesn't allow cross-language, the supplied code is dropped silently
        _project.AllowCrossLanguageIdeas = false;
        _moderationResolver.Setup(m => m.ResolveStatusAsync(It.IsAny<string>(), It.IsAny<string>(), ProjectId, _platformConfig, It.IsAny<CancellationToken>(), It.IsAny<string>()))
            .ReturnsAsync(ModerationStatus.Approved);
        _ideaManager.Setup(i => i.AddIdeaAsync(CategoryId, ProjectId, "desc", ModerationStatus.Approved, _participant, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AddIdeaResult(14, false))
            .Verifiable(Times.Exactly(1));

        // Act
        await _manager.AddModeratedIdeaAsync(_project, CategoryId, "desc", null,
            tempImageUrl: null, imageModerationToken: null, languageCode: "fr",
            _participant, _platformConfig, PromoteNotCalled);

        // Assert
        _ideaManager.VerifyAll();
    }

    [Fact]
    public async Task AddModeratedIdeaAsync_ForwardsCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;
        _moderationResolver.Setup(m => m.ResolveStatusAsync("desc", null, ProjectId, _platformConfig, ct, It.IsAny<string>()))
            .ReturnsAsync(ModerationStatus.Approved)
            .Verifiable(Times.Exactly(1));
        _ideaManager.Setup(i => i.AddIdeaAsync(CategoryId, ProjectId, "desc", ModerationStatus.Approved, _participant, null, null, ct))
            .ReturnsAsync(new AddIdeaResult(15, false))
            .Verifiable(Times.Exactly(1));

        // Act
        await _manager.AddModeratedIdeaAsync(_project, CategoryId, "desc", null,
            tempImageUrl: null, imageModerationToken: null, languageCode: null,
            _participant, _platformConfig, PromoteNotCalled, ct: ct);

        // Assert
        _moderationResolver.VerifyAll();
        _ideaManager.VerifyAll();
    }
}
