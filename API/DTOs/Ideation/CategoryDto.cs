namespace SynapseEnqueteManagement.API.DTOs.Ideation;

public record CategoryDto
{
    public int CategoryId { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
    public int IdeaCount { get; init; }
    public string LanguageCode { get; init; }
    public ICollection<CategoryTranslationDto> Translations { get; init; } = new List<CategoryTranslationDto>();
}
