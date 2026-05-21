using System.ComponentModel.DataAnnotations;
namespace SynapseEnqueteManagement.UI.Admin.Models.Section;

public record SectionFormViewModel
{
    public int SectionId { get; init; }

    [Required]
    [MaxLength(20)]
    public string Name { get; init; }

    [MaxLength(100)]
    public string Description { get; init; }
}
