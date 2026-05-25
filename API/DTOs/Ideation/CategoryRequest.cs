using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.API.DTOs.Ideation;

public record CreateCategoryDto
{
    [Required] [MinLength(1)] public ICollection<CategoryTranslationDto> Translations { get; init; } = new List<CategoryTranslationDto>();
}

public record UpdateCategoryDto
{
    [Required] [MinLength(1)] public ICollection<CategoryTranslationDto> Translations { get; init; } = new List<CategoryTranslationDto>();
}
