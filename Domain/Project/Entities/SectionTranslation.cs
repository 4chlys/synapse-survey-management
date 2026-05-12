using System.ComponentModel.DataAnnotations;
using SynapseEnqueteManagement.BL.Domain.I18n.Entities;

namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

public class SectionTranslation
{
    [Required] public Section Section { get; set; }
    [Required] public Language Language { get; set; }
    [Required] [MaxLength(20)] public string Name { get; set; }
    [MaxLength(100)] public string Description { get; set; }
}
