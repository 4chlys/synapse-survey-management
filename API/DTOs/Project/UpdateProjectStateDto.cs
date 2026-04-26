using System.ComponentModel.DataAnnotations;
namespace SynapseEnqueteManagement.API.DTOs.Project;

public class UpdateProjectStateDto
{
    [Required]
    [MaxLength(20)]
    public string State { get; set; }
}
