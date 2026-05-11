using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.BL.Domain.I18n.Entities;

public class Language
{
    [Key] [MaxLength(10)] public string Code { get; set; }
    [Required] [MaxLength(50)] public string DisplayName { get; set; }
    public bool IsActive { get; set; } = true;
}
