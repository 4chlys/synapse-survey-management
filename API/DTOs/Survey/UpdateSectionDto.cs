using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.API.DTOs.Survey;

public record UpdateSectionDto
{
    [Required]
    public ICollection<SectionTranslationDto> Translations { get; init; } = new List<SectionTranslationDto>();
}
