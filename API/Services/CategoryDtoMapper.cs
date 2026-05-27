using SynapseEnqueteManagement.API.DTOs.Ideation;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.API.Services;

public static class CategoryDtoMapper
{
    public static CategoryDto ToDto(Category category, int ideaCount, string languageCode)
    {
        var t = category.Translations.FirstOrDefault(t => t.Language?.Code == languageCode)
                ?? category.Translations.FirstOrDefault(t => t.Language?.Code == category.Project?.DefaultLanguage?.Code)
                ?? category.Translations.FirstOrDefault();
        return new CategoryDto
        {
            CategoryId = category.CategoryId,
            Name = t?.Name ?? string.Empty,
            Description = t?.Description ?? string.Empty,
            IdeaCount = ideaCount,
            LanguageCode = t?.Language?.Code ?? languageCode,
        };
    }

    public static AdminCategoryDto ToAdminDto(Category category, int ideaCount) => new()
    {
        CategoryId = category.CategoryId,
        IdeaCount = ideaCount,
        Translations = category.Translations.Select(t => new CategoryTranslationDto
        {
            LanguageCode = t.Language?.Code,
            Name = t.Name,
            Description = t.Description,
        }).ToList(),
    };
}
