using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.API.DTOs.Ideation;

public record CategoryTranslationDto
{
    [Required] [MaxLength(10)] public string LanguageCode { get; init; }
    [Required] [MaxLength(100)] public string Name { get; init; }
    [MaxLength(1000)] public string Description { get; init; }
}
