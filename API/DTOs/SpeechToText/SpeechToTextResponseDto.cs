namespace SynapseEnqueteManagement.API.DTOs.SpeechToText;

public sealed class SpeechToTextResponseDto
{
    /// <summary>Transcribed text — null when <see cref="IsSuccess"/> is false.</summary>
    public string Transcript { get; set; }

    public bool IsSuccess { get; set; }

    /// <summary>User-facing error message — null when <see cref="IsSuccess"/> is true.</summary>
    public string ErrorMessage { get; set; }

    /// <summary>
    /// Machine-readable error code; the frontend maps this to a localized message.
    /// Null when <see cref="IsSuccess"/> is true.
    /// </summary>
    public string ErrorCode { get; set; }
}
