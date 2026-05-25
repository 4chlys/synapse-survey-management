namespace SynapseEnqueteManagement.API.DTOs.Ideation;

public record AdminCategoryDto
{
    public int CategoryId { get; init; }
    public int IdeaCount { get; init; }
    public ICollection<CategoryTranslationDto> Translations { get; init; } = new List<CategoryTranslationDto>();
}
