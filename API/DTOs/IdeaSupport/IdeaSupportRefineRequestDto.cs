using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.API.DTOs.IdeaSupport;

public class IdeaSupportRefineRequestDto
{
    [Required]
    [StringLength(1000, MinimumLength = 1)]
    public string DraftText { get; set; } = string.Empty;
}