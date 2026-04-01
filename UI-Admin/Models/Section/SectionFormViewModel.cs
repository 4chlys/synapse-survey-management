using System.ComponentModel.DataAnnotations;
namespace SynapseEnqueteManagement.UI.Admin.Models.Section;

public class SectionFormViewModel
{
    public int SectionId { get; set; }

    [Required(ErrorMessage = "Naam is verplicht.")]
    [MaxLength(20)]
    public string Name { get; set; }

    [MaxLength(100)]
    public string Description { get; set; }
}