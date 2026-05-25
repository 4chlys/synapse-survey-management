using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.API.DTOs.Email;

public record SendEmailDto(
    [Required][EmailAddress][MaxLength(200)] string To,
    [Required][MaxLength(200)] string Subject,
    [Required][MaxLength(5000)] string Body);
