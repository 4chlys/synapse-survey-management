using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.API.DTOs.Project;

public class CopyProjectDto
{
    [MaxLength(30)] public string NewShortForm { get; set; }
    [Range(1, int.MaxValue)] public int PlatformId { get; set; }
    [Required] [MinLength(1)] public ICollection<ProjectTranslationDto> Translations { get; set; } = new List<ProjectTranslationDto>();
}
