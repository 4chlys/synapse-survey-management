using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.API.DTOs.AiSupport;

public record AiSupportMessageDto
{
    [Required, RegularExpression("^(user|assistant)$", ErrorMessage = "Role must be 'user' or 'assistant'.")]
    public string Role    { get; init; } = string.Empty;

    [Required, MaxLength(2000)]
    public string Content { get; init; } = string.Empty;
}
