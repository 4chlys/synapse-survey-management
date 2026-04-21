using System.ComponentModel.DataAnnotations;
namespace SynapseEnqueteManagement.UI.Admin.Models.Project;

public class CreateProjectViewModel
{
    public int? ProjectId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; }

    [MaxLength(100)]
    public string Topic { get; set; }

    [MaxLength(5000)]
    public string Introduction { get; set; }

    public string InteractionMode { get; set; } = "Scroll";

    public string AiAssistance { get; set; } = "ProActive";

    public int? MinAge { get; set; }

    public int? MaxAge { get; set; }

    [MaxLength(500)]
    public string ImageUrl { get; set; }

    public List<string> Sections { get; set; } = new();

    public bool ContactOptIn { get; set; } = true;
}
