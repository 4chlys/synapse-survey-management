using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.API.DTOs.Survey;

public class SectionTranslationDto
{
    [Required] [MaxLength(10)] public string LanguageCode { get; set; }
    [Required] [MaxLength(20)] public string Name { get; set; }
    [MaxLength(100)] public string Description { get; set; }
}
