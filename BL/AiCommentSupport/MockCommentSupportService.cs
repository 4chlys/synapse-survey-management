namespace SynapseEnqueteManagement.BL.AiCommentSupport;

/// <summary>
/// Local-only implementation used when <c>use_ai = false</c> in ai-config.json.
/// Returns scripted responses that exercise the full request/response contract.
/// </summary>
public sealed class MockCommentSupportService : IAiCommentSupportService
{
    public Task<CommentSupportReply> ChatAsync(
        CommentSupportRequest request,
        CancellationToken ct = default)
    {
        if (request.IsFirstTurn || request.History.Count == 0)
        {
            var ideaSnippet = request.IdeaText.Length > 80
                ? request.IdeaText[..80] + "…"
                : request.IdeaText;

            return Task.FromResult(new CommentSupportReply
            {
                AssistantMessage =
                    $"Dit idee gaat over: \"{ideaSnippet}\". " +
                    "Wat is jouw reactie hierop? Typ je reactie hieronder en ik geef je snel feedback.",
                IsFinal = false,
            });
        }

        var userComment = request.UserMessage.Trim();
        var suggestion  = userComment.Length > 5
            ? char.ToUpperInvariant(userComment[0]) + userComment[1..].TrimEnd('.') + "."
            : null;

        return Task.FromResult(new CommentSupportReply
        {
            AssistantMessage =
                "Goede reactie! " +
                (suggestion is not null
                    ? "Ik heb de formulering iets aangescherpt — je kunt dit gebruiken of je eigen versie indienen."
                    : "Je reactie is helder verwoord. Je kunt hem zo indienen."),
            Suggestion = suggestion != userComment ? suggestion : null,
            IsFinal    = true,
        });
    }
}
