using System.ComponentModel.DataAnnotations;
namespace SynapseEnqueteManagement.API.DTOs.Ideation;

public class UpdateCategoryDto
{
    [Required][MaxLength(100)]
    public string Name { get; set; }

    [MaxLength(1000)]
    public string Description { get; set; }
}