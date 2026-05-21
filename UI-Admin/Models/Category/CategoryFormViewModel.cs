using System.ComponentModel.DataAnnotations;
namespace SynapseEnqueteManagement.UI.Admin.Models.Category;

public record CategoryFormViewModel
{
    public int CategoryId { get; init; }

    [Required(ErrorMessage = "Naam is verplicht.")]
    [MaxLength(100)]
    public string Name { get; init; }

    [MaxLength(1000)]
    public string Description { get; init; }
}
