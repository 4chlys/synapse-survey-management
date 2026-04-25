namespace SynapseEnqueteManagement.BL.AiSpeechToText;

/// <summary>Audio data + metadata sent to the transcription service.</summary>
public sealed class SpeechToTextRequest
{
    /// <summary>Raw audio bytes to transcribe.</summary>
    public byte[] AudioData { get; init; } = [];

    /// <summary>
    /// MIME type of the audio (e.g. "audio/webm;codecs=opus").
    /// Gemini accepts: audio/webm, audio/ogg, audio/mp4, audio/wav, audio/flac, audio/mpeg.
    /// </summary>
    public string MimeType { get; init; } = "audio/webm;codecs=opus";
}

/// <summary>Outcome of a transcription call.</summary>
public sealed class SpeechToTextResult
{
    public bool IsSuccess { get; private init; }

    /// <summary>Transcribed text — populated when <see cref="IsSuccess"/> is true.</summary>
    public string Transcript { get; private init; }

    /// <summary>User-facing error message — populated when <see cref="IsSuccess"/> is false.</summary>
    public string ErrorMessage { get; private init; }

    public static SpeechToTextResult Success(string transcript) =>
        new() { IsSuccess = true, Transcript = transcript };

    public static SpeechToTextResult Failed(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };
}
