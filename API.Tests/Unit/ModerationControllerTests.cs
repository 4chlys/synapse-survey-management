using SynapseEnqueteManagement.API.Ai;
using SynapseEnqueteManagement.API.DTOs.Moderation;
using SynapseEnqueteManagement.BL.Ai;
using SynapseEnqueteManagement.BL.AiModeration;
using SynapseEnqueteManagement.BL.Domain.I18n.Entities;
using SynapseEnqueteManagement.BL.Helpers;

namespace SynapseEnqueteManagement.API.Tests;

/// <summary>
/// Integration-style tests for the rewrite-suggestion endpoint: they exercise the real
/// <see cref="ContentLanguageResolver"/> behind the project manager so the language fed to
/// <c>SuggestRewriteAsync</c> reflects the actual content-language resolution, not a stub.
/// </summary>
public class ModerationControllerTests
{
    private readonly Mock<IRegexModerationService> _regexModeration = new();
    private readonly Mock<IAiModerationService> _aiModeration = new();
    private readonly Mock<IModerationTokenService> _tokenService = new();
    private readonly Mock<IParticipantManager> _participantManager = new();
    private readonly Mock<IProjectManager> _projectManager = new();
    private readonly Mock<IPlatformAiConfigService> _platformAiConfig = new();
    private readonly ModerationController _controller;

    private const int ProjectId = 1;
    private readonly Guid _sessionToken = Guid.NewGuid();
    private string _capturedLanguage;

    private static readonly Language Nl = new() { Code = "nl", DisplayName = "Nederlands", IsActive = true };
    private static readonly Language En = new() { Code = "en", DisplayName = "English", IsActive = true };
    private static readonly Language Fr = new() { Code = "fr", DisplayName = "Français", IsActive = true };

    public ModerationControllerTests()
    {
        // Resolution is genuine: the manager wrapper delegates to the real resolver.
        _projectManager
            .Setup(m => m.ResolveContentLanguageCode(It.IsAny<Participant>(), It.IsAny<Project>(), It.IsAny<string>()))
            .Returns((Participant p, Project pr, string code) => ContentLanguageResolver.Resolve(p, pr, code)?.Code);

        _platformAiConfig
            .Setup(s => s.ResolveAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResolvedPlatformAiConfig(
                Mock.Of<Microsoft.Extensions.AI.IChatClient>(),
                "gemini-2.0-flash",
                new ModerationConfig(),
                new SynapseEnqueteManagement.BL.AiIdeaSupport.AiIdeaSupportConfig(),
                new SynapseEnqueteManagement.BL.AiCommentSupport.AiCommentSupportConfig(),
                new SynapseEnqueteManagement.BL.AiQuestionSupport.AiQuestionSupportConfig()));

        _aiModeration
            .Setup(a => a.SuggestRewriteAsync(
                It.IsAny<string>(), It.IsAny<ResolvedPlatformAiConfig>(), ProjectId,
                It.IsAny<CancellationToken>(), It.IsAny<string>()))
            .Callback<string, ResolvedPlatformAiConfig, int, CancellationToken, string>(
                (_, _, _, _, lang) => _capturedLanguage = lang)
            .ReturnsAsync("rewritten");

        _tokenService
            .Setup(t => t.GenerateToken(It.IsAny<string>(), It.IsAny<ModerationStatus>(), ProjectId))
            .Returns("tok");

        _controller = new ModerationController(
            _regexModeration.Object, _aiModeration.Object, _tokenService.Object,
            _participantManager.Object, _projectManager.Object, _platformAiConfig.Object);
        _controller.AttachHttpContext(_sessionToken);
    }

    private Task<ActionResult<SuggestResponseDto>> RunSuggest(
        Language participantLanguage, List<Language> supported, Language defaultLanguage, string dropdown)
    {
        var participant = new Participant
        {
            ParticipantId = 5,
            Language = participantLanguage,
            Project = new Project { ProjectId = ProjectId },
        };
        _participantManager
            .Setup(p => p.GetParticipantBySessionTokenForUpdateAsync(_sessionToken))
            .ReturnsAsync(participant);

        _projectManager
            .Setup(m => m.GetProjectByIdReadOnlyAsync(ProjectId))
            .ReturnsAsync(new Project
            {
                ProjectId = ProjectId,
                ProjectPlatformId = 7,
                SupportedLanguages = supported,
                DefaultLanguage = defaultLanguage,
            });

        return _controller.Suggest(
            ProjectId,
            new ModerationRequestDto { Text = "vies bericht", Language = dropdown },
            CancellationToken.None);
    }

    [Fact]
    public async Task Suggest_EnglishParticipantOnEnglishSupportingProject_RewritesInEnglish()
    {
        await RunSuggest(En, [Nl, En], Nl, dropdown: null);

        Assert.Equal("en", _capturedLanguage);
    }

    [Fact]
    public async Task Suggest_EnglishParticipantOnDutchOnlyProject_RewritesInDutch()
    {
        await RunSuggest(En, [Nl], Nl, dropdown: null);

        Assert.Equal("nl", _capturedLanguage);
    }

    [Fact]
    public async Task Suggest_CrossLanguageProject_FollowsDropdownSelection()
    {
        await RunSuggest(Nl, [Nl, Fr], Nl, dropdown: "fr");

        Assert.Equal("fr", _capturedLanguage);
    }

    [Fact]
    public async Task Suggest_ReturnsSuggestionAndToken()
    {
        var action = await RunSuggest(En, [Nl, En], Nl, dropdown: null);

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        var dto = Assert.IsType<SuggestResponseDto>(ok.Value);
        Assert.Equal("rewritten", dto.Suggestion);
        Assert.Equal("tok", dto.ModerationToken);
    }

    [Fact]
    public async Task Suggest_MissingParticipantToken_ReturnsUnauthorized()
    {
        var controller = new ModerationController(
            _regexModeration.Object, _aiModeration.Object, _tokenService.Object,
            _participantManager.Object, _projectManager.Object, _platformAiConfig.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var action = await controller.Suggest(
            ProjectId, new ModerationRequestDto { Text = "vies bericht" }, CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(action.Result);
    }
}
