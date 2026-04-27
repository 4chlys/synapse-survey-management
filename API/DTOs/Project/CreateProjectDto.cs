using System.ComponentModel.DataAnnotations;
namespace SynapseEnqueteManagement.API.DTOs.Project;

public class CreateProjectDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; }

    [MaxLength(100)]
    public string Topic { get; set; }

    [MaxLength(5000)]
    public string Introduction { get; set; }

    [MaxLength(500)]
    public string ImageUrl { get; set; }

    [MaxLength(20)]
    public string InteractionMode { get; set; }

    [MaxLength(20)]
    public string AiAssistance { get; set; }

    public int? MinAge { get; set; }

    public int? MaxAge { get; set; }

    public bool RequestContactInfo { get; set; }

    [Required]
    public int PlatformId { get; set; }
}
