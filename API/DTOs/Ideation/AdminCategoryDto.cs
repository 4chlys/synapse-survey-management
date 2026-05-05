namespace SynapseEnqueteManagement.API.DTOs.Ideation;

public class AdminCategoryDto
{
    public int CategoryId { get; set; }
    public int IdeaCount { get; set; }
    public ICollection<CategoryTranslationDto> Translations { get; set; } = new List<CategoryTranslationDto>();
}
