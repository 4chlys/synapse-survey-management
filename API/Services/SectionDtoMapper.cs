using SynapseEnqueteManagement.API.DTOs.Survey;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Helpers;

namespace SynapseEnqueteManagement.API.Services;

public static class SectionDtoMapper
{
    public static SectionDto ToDto(Section section, string languageCode)
    {
        var translation = TranslationPicker.Pick(section.Translations, languageCode);

        return new SectionDto
        {
            SectionId = section.SectionId,
            Name = translation?.Name,
            Description = translation?.Description,
            Translations = section.Translations?.Select(t => new SectionTranslationDto
            {
                LanguageCode = t.Language?.Code,
                Name = t.Name,
                Description = t.Description
            }).ToList() ?? new List<SectionTranslationDto>()
        };
    }

    public static AdminSectionDto ToAdminDto(Section section)
        => new()
        {
            SectionId = section.SectionId,
            Translations = section.Translations?.Select(t => new SectionTranslationDto
            {
                LanguageCode = t.Language?.Code,
                Name = t.Name,
                Description = t.Description
            }).ToList() ?? new List<SectionTranslationDto>()
        };
}
