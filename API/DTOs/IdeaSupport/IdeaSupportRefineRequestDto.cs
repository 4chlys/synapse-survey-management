using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.API.DTOs.IdeaSupport;

public record IdeaSupportRefineRequestDto
{
    [Required]
    [MaxLength(1000)]
    public string DraftText { get; init; } = string.Empty;
}
