using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.API.DTOs.Project;

public class SaveContactInfoDto
{
    [EmailAddress]
    [MaxLength(200)]
    public string Email { get; set; }
}
