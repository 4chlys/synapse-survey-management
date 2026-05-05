using System.ComponentModel.DataAnnotations;
using SynapseEnqueteManagement.BL.Domain.I18n.Entities;

namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

public class ProjectTranslation
{
    [Required] public Project Project { get; set; }
    [Required] public Language Language { get; set; }

    [Required] [MaxLength(100)] public string Name { get; set; }
    [MaxLength(100)] public string Topic { get; set; }
}
