using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using SynapseEnqueteManagement.BL.AiSupport;
using SynapseEnqueteManagement.BL.AiUsage;
using SynapseEnqueteManagement.BL.Domain.Ai.Entities;

namespace SynapseEnqueteManagement.BL.AiSpeechToText;

/// <summary>
/// Transcribes audio via <see cref="IChatClient"/> using multimodal input.
/// Sends audio bytes and a transcription prompt; returns the model's text response as the transcript.
/// </summary>
public sealed class AiSpeechToTextService(
    IChatClient chatClient,
    ILogger<AiSpeechToTextService> logger,
    AiSpeechToTextConfig config,
    IAiUsageManager usageManager,
    AiChatModelOptions modelOptions) : IAiSpeechToTextService
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
        int projectId,
        CancellationToken ct = default)
    {
        // Strip codec parameters for the base-type check (e.g. "audio/webm;codecs=opus" → "audio/webm").
        var baseType = request.MimeType.Split(';')[0].Trim();
        if (!SupportedBaseMimeTypes.Contains(baseType))
            return SpeechToTextResult.Failed(
                SpeechToTextErrorCodes.UnsupportedFormat,
                $"Audio format '{baseType}' is not supported.");

        if (request.AudioData.Length == 0)
            return SpeechToTextResult.Failed(
                SpeechToTextErrorCodes.EmptyAudio,
                "No audio data received.");

        if (request.AudioData.Length > config.MaxAudioSizeBytes)
            return SpeechToTextResult.Failed(
                SpeechToTextErrorCodes.TooLong,
                "Recording exceeds the configured maximum length.");

        try
        {
            // Build a multimodal message: audio blob + transcription instruction.
            var audioContent = new DataContent(
                new ReadOnlyMemory<byte>(request.AudioData),
                request.MimeType);

            var prompt = config.TranscriptionPrompt
                .Replace("{language}", GetLanguageName(request.Language), StringComparison.OrdinalIgnoreCase);

            var message = new ChatMessage(ChatRole.User,
            [
                audioContent,
                new TextContent(prompt),
            ]);

            var response = await chatClient.GetResponseAsync(
                [message], new ChatOptions { MaxOutputTokens = 1000 }, ct);

            try
            {
                await usageManager.LogChatAsync(projectId, AiServiceKind.SpeechToText, modelOptions.Model, response.Usage, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[SpeechToText] Failed to record AI usage.");
            }

            var transcript = response.Messages.LastOrDefault()?.Text?.Trim();

            if (string.IsNullOrWhiteSpace(transcript))
                return SpeechToTextResult.Failed(
                    SpeechToTextErrorCodes.NoSpeech,
                    "No speech recognised in the recording.");

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
                SpeechToTextErrorCodes.ServiceUnavailable,
                "Transcription service is currently unavailable.");
        }
    }

    private static string GetLanguageName(string langCode)
    {
        try { return System.Globalization.CultureInfo.GetCultureInfo(langCode).EnglishName; }
        catch { return langCode; }
    }
}
