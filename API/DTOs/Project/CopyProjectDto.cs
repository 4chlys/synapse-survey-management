using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.API.DTOs.Project;

public record CopyProjectDto
{
    [MaxLength(30)] public string NewShortForm { get; init; }
    [Range(1, int.MaxValue)] public int PlatformId { get; init; }
    [Required] [MinLength(1)] public ICollection<ProjectTranslationDto> Translations { get; init; } = new List<ProjectTranslationDto>();
}
