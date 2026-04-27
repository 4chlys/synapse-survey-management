using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.API.DTOs.IdeaSupport;

public class IdeaSupportRefineRequestDto
{
    [Required]
    [MaxLength(1000)]
    public string DraftText { get; set; } = string.Empty;
}
