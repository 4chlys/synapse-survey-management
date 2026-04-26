using System.ComponentModel.DataAnnotations;
namespace SynapseEnqueteManagement.API.DTOs.Project;

public class UpdateProjectDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; }

    [Required]
    [MaxLength(30)]
    [RegularExpression(@"^[a-z0-9](-?[a-z0-9])*$",
        ErrorMessage = "ShortForm must be lowercase alphanumeric with single hyphens.")]
    public string ShortForm { get; set; }

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
}
