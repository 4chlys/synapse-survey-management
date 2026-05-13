using Microsoft.Extensions.Localization;

namespace SynapseEnqueteManagement.BL.AiCommentSupport;

/// <summary>
/// Local-only implementation used when <c>use_ai = false</c> in ai-config.json.
/// Returns scripted responses that exercise the full request/response contract.
/// </summary>
public sealed class MockCommentSupportService(IStringLocalizer<SharedResource> localizer)
    : IAiCommentSupportService
{
    public Task<CommentSupportReply> ChatAsync(
        CommentSupportRequest request,
        int projectId,
        CancellationToken ct = default)
    {
        if (request.IsFirstTurn || request.History.Count == 0)
        {
            var ideaSnippet = request.IdeaText.Length > 80
                ? request.IdeaText[..80] + "…"
                : request.IdeaText;

            return Task.FromResult(new CommentSupportReply
            {
                AssistantMessage = localizer["Mock_Comment_FirstTurn", ideaSnippet].Value,
                IsFinal          = false,
            });
        }

        var userComment = request.UserMessage.Trim();
        var suggestion  = userComment.Length > 5
            ? char.ToUpperInvariant(userComment[0]) + userComment[1..].TrimEnd('.') + "."
            : null;

        return Task.FromResult(new CommentSupportReply
        {
            AssistantMessage = suggestion is not null
                ? localizer["Mock_Comment_GoodReaction_WithSuggestion"].Value
                : localizer["Mock_Comment_GoodReaction_NoSuggestion"].Value,
            Suggestion = suggestion != userComment ? suggestion : null,
            IsFinal    = true,
        });
    }
}
