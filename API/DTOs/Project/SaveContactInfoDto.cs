using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.API.DTOs.Project;

public record SaveContactInfoDto
{
    [EmailAddress]
    [MaxLength(200)]
    public string Email { get; init; }
}
