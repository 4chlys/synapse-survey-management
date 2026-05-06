namespace SynapseEnqueteManagement.API.DTOs.Survey;

public class AdminSectionDto
{
    public int SectionId { get; set; }
    public ICollection<SectionTranslationDto> Translations { get; set; } = new List<SectionTranslationDto>();
}
