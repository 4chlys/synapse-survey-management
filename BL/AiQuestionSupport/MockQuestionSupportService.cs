namespace SynapseEnqueteManagement.BL.AiQuestionSupport;

public sealed class MockQuestionSupportService : IAiQuestionSupportService
{
    private static readonly string[] OpeningMessages =
    [
        "Goed begin! Op basis van je projectomschrijving stel ik voor te starten met een vraag over de huidige situatie. Bijvoorbeeld: 'Hoe tevreden ben je op dit moment over [onderwerp]?' (Likert-schaal).",
        "Ik zie dat je project nog geen vragen heeft. Een goede eerste vraag is vaak een open vraag zoals: 'Wat zou jij willen verbeteren aan [onderwerp]?'",
        "Op basis van je project stel ik voor: 'In welke mate ervaar je [onderwerp] als een probleem?' op een schaal van 1 tot 5.",
    ];

    private static readonly string[] FollowUpMessages =
    [
        "Goede vraag! Overweeg ook een vervolgvraag om het antwoord te verdiepen.",
        "Voor een evenwichtige enquête is een mix van open vragen en meerkeuzevragen ideaal.",
        "Dit klinkt als een sterke vraag. Wil je ook een controlerende vraag toevoegen?",
    ];

    public Task<QuestionSupportChatReply> ChatAsync(
        QuestionSupportChatRequest request,
        CancellationToken ct = default)
    {
        var msg = request.IsFirstTurn || request.History.Count == 0
            ? OpeningMessages[Math.Abs(request.ProjectName.GetHashCode()) % OpeningMessages.Length]
            : FollowUpMessages[request.History.Count % FollowUpMessages.Length];

        return Task.FromResult(new QuestionSupportChatReply { AssistantMessage = msg });
    }
}
