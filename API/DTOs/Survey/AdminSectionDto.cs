namespace SynapseEnqueteManagement.API.DTOs.Survey;

public record AdminSectionDto
{
    public int SectionId { get; init; }
    public ICollection<SectionTranslationDto> Translations { get; init; } = new List<SectionTranslationDto>();
}
