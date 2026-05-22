using System.ComponentModel.DataAnnotations;
using SynapseEnqueteManagement.UI.Admin.Models.Email;
namespace SynapseEnqueteManagement.UI.Admin.Models.Project;

public record CreateProjectViewModel
{
    public int? ProjectId { get; init; }

    [Required]
    [MaxLength(100)]
    public string Name { get; init; }

    [MaxLength(30)]
    [RegularExpression(@"^[a-z0-9](-?[a-z0-9])*$",
        ErrorMessage = "ShortForm_Invalid")]
    public string ShortForm { get; init; }

    [MaxLength(100)]
    public string Topic { get; init; }

    [MaxLength(5000, ErrorMessage = "Introduction_TooLong")]
    public string Introduction { get; init; }

    public string InteractionMode { get; init; } = "VerticalScroll";

    public string AiAssistance { get; init; } = "ProActive";

    [Range(0, 120)]
    public int? MinAge { get; init; }

    [Range(0, 120)]
    public int? MaxAge { get; init; }

    [MaxLength(500)]
    public string ImageUrl { get; init; }

    public IList<string> Sections { get; init; } = new List<string>();

    public bool ContactOptIn { get; init; } = true;
    public bool AllowCrossLanguageIdeas { get; init; } = false;

    [Required]
    [MaxLength(10)]
    public string DefaultLanguageCode { get; init; } = "nl";

    public ICollection<string> SupportedLanguageCodes { get; init; } = new List<string>();

    [MaxLength(20)]
    public string Theme { get; init; } = "Default";

    [MaxLength(7)]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$",
        ErrorMessage = "PrimaryColor_Invalid")]
    public string PrimaryColor { get; init; }

    [MaxLength(4000)]
    public string CustomCss { get; init; }

    public EmailTemplateViewModel EmailTemplate { get; init; } = new(string.Empty, string.Empty, false);
}
