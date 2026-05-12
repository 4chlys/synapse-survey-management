using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.API.DTOs.Survey;

public class UpdateSectionDto
{
    [Required]
    public ICollection<SectionTranslationDto> Translations { get; set; } = new List<SectionTranslationDto>();
}
