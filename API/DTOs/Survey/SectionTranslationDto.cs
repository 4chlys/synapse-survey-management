using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.API.DTOs.Survey;

public record SectionTranslationDto
{
    [Required] [MaxLength(10)] public string LanguageCode { get; init; }
    [Required] [MaxLength(20)] public string Name { get; init; }
    [MaxLength(100)] public string Description { get; init; }
}
