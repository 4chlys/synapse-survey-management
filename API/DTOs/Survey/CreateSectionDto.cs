using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.API.DTOs.Survey;

public class CreateSectionDto
{
    [Required]
    public ICollection<SectionTranslationDto> Translations { get; set; } = new List<SectionTranslationDto>();
}
