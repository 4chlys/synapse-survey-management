using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.API.DTOs.Moderation;

public sealed record ModerationRequestDto
{
    [Required] [MaxLength(1000)]
    public string Text { get; init; }

    [MaxLength(20)]
    public string ContentType { get; init; } = "idea";

    [MaxLength(10)]
    public string Language { get; init; }
}
