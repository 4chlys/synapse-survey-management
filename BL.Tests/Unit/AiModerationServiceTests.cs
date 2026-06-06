using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using SynapseEnqueteManagement.BL.Ai;
using SynapseEnqueteManagement.BL.AiCommentSupport;
using SynapseEnqueteManagement.BL.AiIdeaSupport;
using SynapseEnqueteManagement.BL.AiModeration;
using SynapseEnqueteManagement.BL.AiQuestionSupport;
using SynapseEnqueteManagement.BL.AiUsage;

namespace SynapseEnqueteManagement.BL.Tests.Unit;

public class AiModerationServiceTests
{
    private const string RewritePromptTemplate = "Schrijf in het {language}. Output in {response_language}.";

    private static (AiModerationService service, Mock<IChatClient> client, List<ChatMessage> captured) MakeService(string reply)
    {
        var captured = new List<ChatMessage>();
        var client = new Mock<IChatClient>();
        client.Setup(c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ChatMessage>, ChatOptions, CancellationToken>((msgs, _, _) => captured.AddRange(msgs))
            .ReturnsAsync(new ChatResponse([new ChatMessage(ChatRole.Assistant, reply)]));

        var service = new AiModerationService(
            NullLogger<AiModerationService>.Instance,
            new Mock<IRegexModerationService>().Object,
            new Mock<IAiUsageManager>().Object);

        return (service, client, captured);
    }

    private static ResolvedPlatformAiConfig MakeConfig(Mock<IChatClient> client) =>
        new(
            client.Object,
            "model-id",
            new ModerationConfig
            {
                UseAi           = true,
                DefaultLanguage = "nl",
                RewritePrompt   = RewritePromptTemplate,
            },
            IdeaSupport: null,
            CommentSupport: null,
            QuestionSupport: null);

    [Fact]
    public async Task SuggestRewriteAsync_SubstitutesBothLanguageTokensWithRequestLanguage()
    {
        var (service, client, captured) = MakeService("Herschreven tekst");

        await service.SuggestRewriteAsync("vies bericht", MakeConfig(client), projectId: 1, respondInLanguage: "en");

        Assert.Equal("Schrijf in het en. Output in en.", captured[0].Text);
    }

    [Fact]
    public async Task SuggestRewriteAsync_AddsExplicitEnglishDirectiveWithFullLanguageName()
    {
        var (service, client, captured) = MakeService("Herschreven tekst");

        await service.SuggestRewriteAsync("vies bericht", MakeConfig(client), projectId: 1, respondInLanguage: "en");

        Assert.Equal("You MUST respond in English. Do not use any other language.", captured[1].Text);
        Assert.Equal("vies bericht", captured[2].Text);
    }

    [Fact]
    public async Task SuggestRewriteAsync_NullRequestLanguage_FallsBackToDefaultLanguage()
    {
        var (service, client, captured) = MakeService("Herschreven tekst");

        await service.SuggestRewriteAsync("vies bericht", MakeConfig(client), projectId: 1, respondInLanguage: null);

        Assert.Equal("Schrijf in het nl. Output in nl.", captured[0].Text);
        Assert.Equal("You MUST respond in Dutch. Do not use any other language.", captured[1].Text);
    }

    [Fact]
    public async Task SuggestRewriteAsync_ModelReturnsNull_ReturnsNull()
    {
        var (service, client, _) = MakeService("NULL");

        var result = await service.SuggestRewriteAsync("vies bericht", MakeConfig(client), projectId: 1, respondInLanguage: "en");

        Assert.Null(result);
    }

    [Fact]
    public async Task SuggestRewriteAsync_BlankText_ReturnsNullWithoutCallingClient()
    {
        var (service, client, _) = MakeService("Herschreven tekst");

        var result = await service.SuggestRewriteAsync("   ", MakeConfig(client), projectId: 1, respondInLanguage: "en");

        Assert.Null(result);
        client.Verify(c => c.GetResponseAsync(
            It.IsAny<IEnumerable<ChatMessage>>(),
            It.IsAny<ChatOptions>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CheckWithAiAsync_RespondInLanguageEn_PromptContainsEnNotPlaceholder()
    {
        // Arrange — platform language "nl", participant language "en"
        var config = new ModerationConfig
        {
            UseAi           = true,
            DefaultLanguage = "nl",
            ModerationPrompt =
                "Assess content. Respond: { \"acceptable\": false, \"reason\": \"<reason in {response_language}>\" }",
        };

        IEnumerable<ChatMessage> capturedMessages = null;
        var mockChat = new Mock<IChatClient>();
        mockChat
            .Setup(c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ChatMessage>, ChatOptions, CancellationToken>(
                (msgs, _, _) => capturedMessages = msgs.ToList())
            .ReturnsAsync(new ChatResponse(
                [new ChatMessage(ChatRole.Assistant, "{\"acceptable\":true}")]));

        var mockRegex = new Mock<IRegexModerationService>();
        mockRegex
            .Setup(r => r.Check(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(ModerationResult.Acceptable());

        var sut = new AiModerationService(
            NullLogger<AiModerationService>.Instance,
            mockRegex.Object,
            new Mock<IAiUsageManager>().Object);

        var platformConfig = new ResolvedPlatformAiConfig(
            mockChat.Object,
            "gemini-2.0-flash",
            config,
            new AiIdeaSupportConfig(),
            new AiCommentSupportConfig(),
            new AiQuestionSupportConfig());

        // Act
        await sut.CheckWithAiAsync("some idea text", platformConfig, 0, respondInLanguage: "en");

        // Assert — "{response_language}" replaced with "en" (not left as placeholder, not "nl")
        Assert.NotNull(capturedMessages);
        var systemPrompt = capturedMessages.First(m => m.Role == ChatRole.System).Text;
        Assert.Contains("en", systemPrompt);
        Assert.DoesNotContain("{response_language}", systemPrompt);
        Assert.DoesNotContain("<reason in nl>", systemPrompt);
    }
}
