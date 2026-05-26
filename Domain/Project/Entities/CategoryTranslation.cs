using System.ComponentModel.DataAnnotations;
using SynapseEnqueteManagement.BL.Domain.I18n.Entities;

namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

public class CategoryTranslation : IHasLanguage
{
    [Required] public Category Category { get; set; }
    [Required] public Language Language { get; set; }
    [Required] [MaxLength(100)] public string Name { get; set; }
    [MaxLength(1000)] public string Description { get; set; }
}
