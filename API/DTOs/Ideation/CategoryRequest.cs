using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.API.DTOs.Ideation;

public class CreateCategoryDto
{
    [Required] [MinLength(1)] public ICollection<CategoryTranslationDto> Translations { get; set; } = new List<CategoryTranslationDto>();
}

public class UpdateCategoryDto
{
    [Required] [MinLength(1)] public ICollection<CategoryTranslationDto> Translations { get; set; } = new List<CategoryTranslationDto>();
}
