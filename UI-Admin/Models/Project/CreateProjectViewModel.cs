using System.ComponentModel.DataAnnotations;
namespace SynapseEnqueteManagement.UI.Admin.Models.Project;

public class CreateProjectViewModel
{
    [Required(ErrorMessage = "Naam is verplicht.")]
    [MaxLength(100, ErrorMessage = "Naam mag maximaal 100 tekens bevatten.")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Topic { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Introduction { get; set; } = string.Empty;

    public string InteractionMode { get; set; } = "Scroll";

    public string AiAssistance { get; set; } = "ProActive";

    public int? MinAge { get; set; }

    public int? MaxAge { get; set; }

    [MaxLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    public List<string> Sections { get; set; } = new();

    public bool ContactOptIn { get; set; } = true;
}