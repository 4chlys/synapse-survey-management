using System.ComponentModel.DataAnnotations;
namespace SynapseEnqueteManagement.UI.Admin.Models.Category;

public class CategoryFormViewModel
{
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Naam is verplicht.")]
    [MaxLength(100)]
    public string Name { get; set; }

    [MaxLength(1000)]
    public string Description { get; set; }
}