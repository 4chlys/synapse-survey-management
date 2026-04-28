using System.Text.Json.Serialization;

namespace SynapseEnqueteManagement.BL.AiSpeechToText;

/// <summary>
/// Configuration block for the Gemini-powered speech-to-text service.
/// Lives as <c>speech_to_text</c> inside ai-config.json.
/// </summary>
public sealed class AiSpeechToTextConfig
{
    /// <summary>
    /// Prompt appended to the audio to instruct Gemini to transcribe.
    /// Supports {language} substitution.
    /// </summary>
    [JsonPropertyName("transcription_prompt")]
    public string TranscriptionPrompt { get; init; } =
        "Transcribeer de volgende audioclip nauwkeurig. " +
        "Geef ALLEEN de transcriptie terug, zonder uitleg of extra tekst.";

    /// <summary>Maximum accepted audio size in bytes. Requests exceeding this are rejected before any AI call.</summary>
    [JsonPropertyName("max_audio_size_bytes")]
    public int MaxAudioSizeBytes { get; init; } = 2_097_152; // 2 MB — ~30 s of WebM/Opus
}
