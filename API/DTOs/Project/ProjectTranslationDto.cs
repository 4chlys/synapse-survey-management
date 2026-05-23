using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.API.DTOs.Project;

public record ProjectTranslationDto
{
    [Required] [MaxLength(10)] public string LanguageCode { get; init; }
    [Required] [MaxLength(100)] public string Name { get; init; }
    [MaxLength(100)] public string Topic { get; init; }
    [MaxLength(5000)] public string Introduction { get; init; }
}
