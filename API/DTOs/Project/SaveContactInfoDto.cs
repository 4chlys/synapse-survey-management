using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.API.DTOs.Project;

public class SaveContactInfoDto
{
    [EmailAddress]
    [StringLength(254)]
    public string Email { get; set; }

    [Phone]
    [StringLength(20)]
    public string PhoneNumber { get; set; }
}
