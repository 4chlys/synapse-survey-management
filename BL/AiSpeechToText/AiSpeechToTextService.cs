using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace SynapseEnqueteManagement.BL.AiSpeechToText;

/// <summary>
/// Transcribes audio via <see cref="IChatClient"/> using multimodal input.
/// Sends audio bytes and a transcription prompt; returns the model's text response as the transcript.
/// </summary>
public sealed class AiSpeechToTextService(
    IChatClient chatClient,
    ILogger<AiSpeechToTextService> logger,
    AiSpeechToTextConfig config) : IAiSpeechToTextService
{
    // Base MIME types accepted by Gemini multimodal audio input.
    private static readonly HashSet<string> SupportedBaseMimeTypes = new(
        StringComparer.OrdinalIgnoreCase)
    {
        "audio/webm", "audio/ogg", "audio/mp4",
        "audio/wav", "audio/flac", "audio/mpeg", "audio/aac",
    };

    public async Task<SpeechToTextResult> TranscribeAsync(
        SpeechToTextRequest request,
        CancellationToken ct = default)
    {
        // Strip codec parameters for the base-type check (e.g. "audio/webm;codecs=opus" → "audio/webm").
        var baseType = request.MimeType.Split(';')[0].Trim();
        if (!SupportedBaseMimeTypes.Contains(baseType))
            return SpeechToTextResult.Failed($"Audioformaat '{baseType}' wordt niet ondersteund.");

        if (request.AudioData.Length == 0)
            return SpeechToTextResult.Failed("Geen audiodata ontvangen.");

        if (request.AudioData.Length > config.MaxAudioSizeBytes)
            return SpeechToTextResult.Failed("De opname is te lang. Houd het onder 30 seconden.");

        try
        {
            // Build a multimodal message: audio blob + transcription instruction.
            var audioContent = new DataContent(
                new ReadOnlyMemory<byte>(request.AudioData),
                request.MimeType);

            var message = new ChatMessage(ChatRole.User,
            [
                audioContent,
                new TextContent(config.TranscriptionPrompt),
            ]);

            var response = await chatClient.GetResponseAsync(
                [message], cancellationToken: ct);

            var transcript = response.Messages.LastOrDefault()?.Text?.Trim();

            if (string.IsNullOrWhiteSpace(transcript))
                return SpeechToTextResult.Failed("Geen spraak herkend in de opname.");

            return SpeechToTextResult.Success(transcript);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[SpeechToText] Transcription failed.");
            return SpeechToTextResult.Failed(
                "Spraakherkenning is momenteel niet beschikbaar. Typ je bericht handmatig.");
        }
    }
}
