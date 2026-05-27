using SynapseEnqueteManagement.API.DTOs.Ideation;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Helpers;

namespace SynapseEnqueteManagement.API.Services;

public static class CategoryDtoMapper
{
    public static CategoryDto ToDto(Category category, int ideaCount, string languageCode)
    {
        var t = TranslationPicker.Pick(category.Translations, languageCode, category.Project?.DefaultLanguage?.Code);
        return new CategoryDto
        {
            CategoryId = category.CategoryId,
            Name = t?.Name ?? string.Empty,
            Description = t?.Description ?? string.Empty,
            IdeaCount = ideaCount,
            LanguageCode = t?.Language?.Code ?? languageCode,
            Translations = category.Translations.Select(tr => new CategoryTranslationDto
            {
                LanguageCode = tr.Language?.Code,
                Name = tr.Name,
                Description = tr.Description,
            }).ToList(),
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
