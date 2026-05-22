using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.API.DTOs.Email;

public record SaveEmailTemplateDto(
    [MaxLength(200)] string Subject,
    [MaxLength(5000)] string Body,
    bool IsEnabled);
