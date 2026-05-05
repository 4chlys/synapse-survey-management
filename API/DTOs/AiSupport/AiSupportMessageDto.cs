using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.API.DTOs.AiSupport;

public class AiSupportMessageDto
{
    [Required, RegularExpression("^(user|assistant)$", ErrorMessage = "Role must be 'user' or 'assistant'.")]
    public string Role    { get; set; } = string.Empty;

    [Required, MaxLength(2000)]
    public string Content { get; set; } = string.Empty;
}
