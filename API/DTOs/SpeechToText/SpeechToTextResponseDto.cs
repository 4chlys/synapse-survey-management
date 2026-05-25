namespace SynapseEnqueteManagement.API.DTOs.SpeechToText;

public sealed record SpeechToTextResponseDto
{
    /// <summary>Transcribed text — null when <see cref="IsSuccess"/> is false.</summary>
    public string Transcript { get; init; }

    public bool IsSuccess { get; init; }

    /// <summary>User-facing error message — null when <see cref="IsSuccess"/> is true.</summary>
    public string ErrorMessage { get; init; }

    /// <summary>
    /// Machine-readable error code; the frontend maps this to a localized message.
    /// Null when <see cref="IsSuccess"/> is true.
    /// </summary>
    public string ErrorCode { get; init; }
}
