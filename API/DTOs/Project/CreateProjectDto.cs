using System.ComponentModel.DataAnnotations;
namespace SynapseEnqueteManagement.API.DTOs.Project;


public class CreateProjectDto
{
    [Required]
    [MaxLength(50)]
    public string Name { get; set; }
    
    [MaxLength(100)]
    [RegularExpression(@"^https?://.*", ErrorMessage = "Only http and https URLsare allowed.")]
    public string ImageUrl { get; set; }
    
}