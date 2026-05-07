namespace SynapseEnqueteManagement.BL.AiSpeechToText;

/// <summary>
/// Local-only implementation used when <c>use_ai = false</c> in ai-config.json.
/// Returns a scripted placeholder so the full request/response contract can be
/// exercised without Vertex AI credentials.
/// </summary>
public sealed class MockSpeechToTextService : IAiSpeechToTextService
{
    private static readonly string[] PlaceholderTranscripts =
    [
        "Dit is een testopname van de spraakherkenning.",
        "Mijn idee is om meer groenvoorzieningen in de wijk te plaatsen.",
        "Ik zou graag een betere fietsinfrastructuur zien op de hoofdstraat.",
    ];

    public Task<SpeechToTextResult> TranscribeAsync(
        SpeechToTextRequest request,
        CancellationToken ct = default)
    {
        if (request.AudioData.Length == 0)
            return Task.FromResult(SpeechToTextResult.Failed(
                SpeechToTextErrorCodes.EmptyAudio,
                "No audio data received."));

        // Deterministic placeholder based on data length so repeated tests give stable output.
        var idx = request.AudioData.Length % PlaceholderTranscripts.Length;
        return Task.FromResult(SpeechToTextResult.Success(PlaceholderTranscripts[idx]));
    }
}
