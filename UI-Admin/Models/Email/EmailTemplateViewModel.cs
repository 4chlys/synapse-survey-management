using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.UI.Admin.Models.Email;

public record EmailTemplateViewModel(
    [MaxLength(200)] string Subject,
    [MaxLength(5000)] string Body,
    bool IsEnabled);
