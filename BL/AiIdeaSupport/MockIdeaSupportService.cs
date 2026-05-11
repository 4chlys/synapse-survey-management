using Microsoft.Extensions.Localization;
using SynapseEnqueteManagement.BL.Ai;

namespace SynapseEnqueteManagement.BL.AiIdeaSupport;

/// <summary>
/// Local-only implementation used when <c>use_ai = false</c> in ai-config.json.
///
/// Returns scripted responses that exercise the full request/response contract
/// without requiring Vertex AI credentials, so developers can work on the
/// front-end chat UI without any cloud dependency.
/// </summary>
public sealed class MockIdeaSupportService(IStringLocalizer<SharedResource> localizer)
    : IAiIdeaSupportService
{
    private static readonly string[] OpeningKeys =
    [
        "Mock_Idea_Opening_1",
        "Mock_Idea_Opening_2",
        "Mock_Idea_Opening_3",
    ];

    private static readonly string[] FollowUpKeys =
    [
        "Mock_Idea_FollowUp_1",
        "Mock_Idea_FollowUp_2",
        "Mock_Idea_FollowUp_3",
        "Mock_Idea_FollowUp_4",
    ];

    public Task<IdeaSupportChatReply> ChatAsync(
        IdeaSupportChatRequest request,
        ResolvedPlatformAiConfig platformConfig,
        CancellationToken ct = default)
    {
        var turnCount = request.History.Count;

        var userTurns = request.History.Count(m =>
            m.Role.Equals("user", StringComparison.OrdinalIgnoreCase)) + 1;

        if (userTurns >= 4)
        {
            var draft = localizer[
                "Mock_Idea_Draft_Body",
                request.CategoryName.ToLowerInvariant(),
                request.UserMessage.TrimEnd('.')].Value;

            return Task.FromResult(new IdeaSupportChatReply
            {
                AssistantMessage = localizer["Mock_Idea_Draft_Intro"].Value,
                SuggestedDraft   = draft,
                Phase            = IdeaSupportPhase.Draft,
            });
        }

        if (request.IsFirstTurn || turnCount == 0)
        {
            var idx = Math.Abs(request.CategoryName.GetHashCode()) % OpeningKeys.Length;
            return Task.FromResult(new IdeaSupportChatReply
            {
                AssistantMessage = localizer[OpeningKeys[idx]].Value,
            });
        }

        var followIdx = turnCount % FollowUpKeys.Length;
        return Task.FromResult(new IdeaSupportChatReply
        {
            AssistantMessage = localizer[FollowUpKeys[followIdx]].Value,
        });
    }

    public Task<IdeaSupportRefineResult> RefineAsync(
        string draftText,
        string language,
        ResolvedPlatformAiConfig platformConfig,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(draftText))
            return Task.FromResult(new IdeaSupportRefineResult { RefinedText = null });

        var refined = char.ToUpperInvariant(draftText[0]) + draftText[1..].TrimEnd();
        if (!refined.EndsWith('.') && !refined.EndsWith('!') && !refined.EndsWith('?'))
            refined += '.';

        return Task.FromResult(new IdeaSupportRefineResult { RefinedText = refined });
    }
}