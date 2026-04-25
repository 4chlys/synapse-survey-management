using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.API.DTOs.SpeechToText;

public sealed class SpeechToTextRequestDto
{
    /// <summary>Base64-encoded audio bytes.</summary>
    [Required]
    public string AudioData { get; set; } = string.Empty;

    /// <summary>
    /// MIME type of the audio (e.g. "audio/webm;codecs=opus").
    /// Must be one of the types supported by Gemini multimodal.
    /// </summary>
    [Required]
    [StringLength(64)]
    public string MimeType { get; set; } = "audio/webm;codecs=opus";
}
