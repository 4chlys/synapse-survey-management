namespace SynapseEnqueteManagement.BL.AiSpeechToText;

/// <summary>Audio data + metadata sent to the transcription service.</summary>
public sealed record SpeechToTextRequest
{
    /// <summary>Raw audio bytes to transcribe.</summary>
    public byte[] AudioData { get; init; } = [];

    /// <summary>
    /// MIME type of the audio (e.g. "audio/webm;codecs=opus").
    /// Gemini accepts: audio/webm, audio/ogg, audio/mp4, audio/wav, audio/flac, audio/mpeg.
    /// </summary>
    public string MimeType { get; init; } = "audio/webm;codecs=opus";

    /// <summary>BCP-47 two-letter language code of the participant's UI language (e.g. "nl", "en").</summary>
    public string Language { get; init; } = "nl";
}

/// <summary>Outcome of a transcription call.</summary>
public sealed record SpeechToTextResult
{
    public bool IsSuccess { get; private init; }

    /// <summary>Transcribed text — populated when <see cref="IsSuccess"/> is true.</summary>
    public string Transcript { get; private init; }

    /// <summary>Developer-facing error message — populated when <see cref="IsSuccess"/> is false.</summary>
    public string ErrorMessage { get; private init; }

    /// <summary>
    /// Machine-readable error code; the frontend maps this to a localized message.
    /// Populated when <see cref="IsSuccess"/> is false.
    /// </summary>
    public string ErrorCode { get; private init; }

    public static SpeechToTextResult Success(string transcript) =>
        new() { IsSuccess = true, Transcript = transcript };

    public static SpeechToTextResult Failed(string errorCode, string errorMessage) =>
        new() { IsSuccess = false, ErrorCode = errorCode, ErrorMessage = errorMessage };
}

/// <summary>Stable error code identifiers; the frontend maps these to localized messages.</summary>
public static class SpeechToTextErrorCodes
{
    public const string UnsupportedFormat  = "unsupported_format";
    public const string EmptyAudio         = "empty_audio";
    public const string TooLong            = "too_long";
    public const string NoSpeech           = "no_speech";
    public const string ServiceUnavailable = "service_unavailable";
    public const string InvalidAudioData   = "invalid_audio_data";
    public const string UnexpectedResponse = "unexpected_response";
}
