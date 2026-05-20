using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.API.DTOs.Email;

public class SaveEmailTemplateDto
{
    [MaxLength(200)] public string Subject { get; set; }
    [MaxLength(5000)] public string Body { get; set; }
    public bool IsEnabled { get; set; }
}
