namespace SynapseEnqueteManagement.BL.AiIdeaSupport;

/// <summary>
/// Local-only implementation used when <c>use_ai = false</c> in ai-config.json.
///
/// Returns scripted responses that exercise the full request/response contract
/// without requiring Vertex AI credentials, so developers can work on the
/// front-end chat UI without any cloud dependency.
/// </summary>
public sealed class MockIdeaSupportService : IAiIdeaSupportService
{
    private static readonly string[] OpeningMessages =
    [
        "Interessant! Wat is het grootste probleem dat jouw idee oplost?",
        "Goed dat je een idee hebt! Kun je me vertellen voor wie dit idee bedoeld is?",
        "Vertel me meer — wat heeft je geïnspireerd om dit idee te bedenken?",
    ];

    private static readonly string[] FollowUpMessages =
    [
        "Dat klinkt goed. Hoe zou jij de eerste stap zetten om dit idee te realiseren?",
        "Interessant perspectief! Wie zou er nog meer baat bij hebben?",
        "Kun je een concreet voorbeeld geven van hoe dit werkt in de praktijk?",
        "Wat zijn mogelijke obstakels en hoe zou je die aanpakken?",
    ];

    private static readonly string[] DraftMessages =
    [
        "Op basis van ons gesprek heb ik een voorstel voor je idee samengesteld:",
    ];

    public Task<IdeaSupportChatReply> ChatAsync(
        IdeaSupportChatRequest request,
        CancellationToken ct = default)
    {
        var turnCount = request.History.Count;

        var userTurns = request.History.Count(m =>
            m.Role.Equals("user", StringComparison.OrdinalIgnoreCase)) + 1;

        if (userTurns >= 4)
        {
            var draftMsg = DraftMessages[0];
            var draft    = $"Ik stel voor om {request.CategoryName.ToLowerInvariant()} te verbeteren door: {request.UserMessage.TrimEnd('.')}. " +
                           "Dit zou een positieve impact hebben op de betrokken mensen en is haalbaar met de juiste aanpak.";

            return Task.FromResult(new IdeaSupportChatReply
            {
                AssistantMessage = draftMsg,
                SuggestedDraft   = draft,
            });
        }
        
        if (request.IsFirstTurn || turnCount == 0)
        {
            var idx = Math.Abs(request.CategoryName.GetHashCode()) % OpeningMessages.Length;
            return Task.FromResult(new IdeaSupportChatReply
            {
                AssistantMessage = OpeningMessages[idx],
            });
        }

        var followIdx = turnCount % FollowUpMessages.Length;
        return Task.FromResult(new IdeaSupportChatReply
        {
            AssistantMessage = FollowUpMessages[followIdx],
        });
    }

    public Task<IdeaSupportRefineResult> RefineAsync(
        string draftText,
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