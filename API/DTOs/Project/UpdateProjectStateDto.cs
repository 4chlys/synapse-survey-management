using System.ComponentModel.DataAnnotations;
namespace SynapseEnqueteManagement.API.DTOs.Project;

public record UpdateProjectStateDto
{
    [Required]
    [MaxLength(20)]
    public string State { get; init; }
}
