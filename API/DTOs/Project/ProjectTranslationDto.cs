using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.API.DTOs.Project;

public class ProjectTranslationDto
{
    [Required] [MaxLength(10)] public string LanguageCode { get; set; }
    [Required] [MaxLength(100)] public string Name { get; set; }
    [MaxLength(100)] public string Topic { get; set; }
    [MaxLength(5000)] public string Introduction { get; set; }
}
