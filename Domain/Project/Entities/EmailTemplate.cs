using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

public class EmailTemplate
{
    public int EmailTemplateId { get; set; }
    [Required] public Project Project { get; set; }
    [MaxLength(200)] public string Subject { get; set; }
    [MaxLength(5000)] public string Body { get; set; }
    public bool IsEnabled { get; set; }
}
