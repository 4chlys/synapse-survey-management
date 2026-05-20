using System.ComponentModel.DataAnnotations;
using SynapseEnqueteManagement.UI.Admin.Models.Email;
namespace SynapseEnqueteManagement.UI.Admin.Models.Project;

public class CreateProjectViewModel
{
    public int? ProjectId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; }

    [MaxLength(30)]
    [RegularExpression(@"^[a-z0-9](-?[a-z0-9])*$",
        ErrorMessage = "ShortForm_Invalid")]
    public string ShortForm { get; set; }

    [MaxLength(100)]
    public string Topic { get; set; }

    [MaxLength(5000, ErrorMessage = "Introduction_TooLong")]
    public string Introduction { get; set; }

    public string InteractionMode { get; set; } = "VerticalScroll";

    public string AiAssistance { get; set; } = "ProActive";

    [Range(0, 120)]
    public int? MinAge { get; set; }

    [Range(0, 120)]
    public int? MaxAge { get; set; }

    [MaxLength(500)]
    public string ImageUrl { get; set; }

    public List<string> Sections { get; set; } = new();

    public bool ContactOptIn { get; set; } = true;
    public bool AllowCrossLanguageIdeas { get; set; } = false;

    [Required]
    [MaxLength(10)]
    public string DefaultLanguageCode { get; set; } = "nl";

    public ICollection<string> SupportedLanguageCodes { get; set; } = new List<string>();

    [MaxLength(20)]
    public string Theme { get; set; } = "Default";

    [MaxLength(7)]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$",
        ErrorMessage = "PrimaryColor_Invalid")]
    public string PrimaryColor { get; set; }

    [MaxLength(4000)]
    public string CustomCss { get; set; }

    public EmailTemplateViewModel EmailTemplate { get; set; } = new();
}
