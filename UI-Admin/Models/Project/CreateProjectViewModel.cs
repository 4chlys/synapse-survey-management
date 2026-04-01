using System.ComponentModel.DataAnnotations;
namespace SynapseEnqueteManagement.UI.Admin.Models.Project;

public class CreateProjectViewModel
{
    [Required(ErrorMessage = "Naam is verplicht.")]
    [MaxLength(50, ErrorMessage = "Naam mag maximaal 50 tekens bevatten.")]
    public string Name { get; set; }

    [MaxLength(100)]
    [Url(ErrorMessage = "Voer een geldige URL in (https://...).")]
    public string ImageUrl { get; set; }
}