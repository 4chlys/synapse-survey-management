using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using SynapseEnqueteManagement.BL.Ai;
using SynapseEnqueteManagement.BL.AiUsage;
using SynapseEnqueteManagement.BL.Domain.Ai.Entities;

namespace SynapseEnqueteManagement.BL.AiSpeechToText;

public sealed class AiSpeechToTextService(
    AiSpeechToTextConfig config,
    IAiUsageManager usageManager,
    ILogger<AiSpeechToTextService> logger) : IAiSpeechToTextService
{
    private static readonly HashSet<string> SupportedBaseMimeTypes = new(
        StringComparer.OrdinalIgnoreCase)
    {
        "audio/webm", "audio/ogg", "audio/mp4",
        "audio/wav", "audio/flac", "audio/mpeg", "audio/aac",
    };

    public async Task<SpeechToTextResult> TranscribeAsync(
        SpeechToTextRequest request,
        ResolvedPlatformAiConfig platformConfig,
        int projectId,
        CancellationToken ct = default)
    {
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
            var audioContent = new DataContent(
                new ReadOnlyMemory<byte>(request.AudioData),
                request.MimeType);

            var langName = GetLanguageName(request.Language);
            var prompt = config.TranscriptionPrompt
                .Replace("{language}", langName, StringComparison.OrdinalIgnoreCase);

            // Explicit language instruction using the full name so it is unambiguous
            // regardless of what language the transcription prompt itself is written in.
            var languageDirective = new ChatMessage(ChatRole.System,
                $"You are a transcription engine. Output the transcription in {langName} only. " +
                $"If the speech is in another language, translate the words into {langName}. " +
                "Return only the transcription text, with no explanation.");

            var message = new ChatMessage(ChatRole.User,
            [
                audioContent,
                new TextContent(prompt),
            ]);

            var response = await platformConfig.ChatClient.GetResponseAsync(
                [languageDirective, message], cancellationToken: ct);

            try { await usageManager.LogChatAsync(projectId, AiServiceKind.SpeechToText, platformConfig.SelectedModelId, response.Usage, ct); }
            catch (Exception ex) { logger.LogWarning(ex, "[SpeechToText] Failed to record AI usage."); }

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