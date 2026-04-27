using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.API.DTOs.Moderation;

public sealed class ModerationRequestDto
{
    [Required] [MaxLength(1000)]
    public string Text { get; set; }

    [MaxLength(20)]
    public string ContentType { get; set; } = "idea";
}
