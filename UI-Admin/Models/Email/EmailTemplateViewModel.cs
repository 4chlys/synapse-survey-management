using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.UI.Admin.Models.Email;

public class EmailTemplateViewModel
{
    [MaxLength(200)] public string Subject { get; set; }
    [MaxLength(5000)] public string Body { get; set; }
    public bool IsEnabled { get; set; }
}
