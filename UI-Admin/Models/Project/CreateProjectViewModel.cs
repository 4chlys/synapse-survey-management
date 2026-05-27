using System.ComponentModel.DataAnnotations;
using SynapseEnqueteManagement.UI.Admin.Models.Section;
namespace SynapseEnqueteManagement.UI.Admin.Models.Project;

public record CreateProjectViewModel
{
    public int? ProjectId { get; init; }

    [MaxLength(30)]
    [RegularExpression(@"^[a-z0-9](-?[a-z0-9])*$",
        ErrorMessage = "ShortForm_Invalid")]
    public string ShortForm { get; init; }

    // One entry per language the form renders (always all platform languages),
    // even if SupportedLanguageCodes excludes some. Server filters on submit.
    public IList<ProjectTranslationViewModel> Translations { get; init; } = new List<ProjectTranslationViewModel>();

    public string InteractionMode { get; init; } = "VerticalScroll";

    [Range(0, 120)]
    public int? MinAge { get; init; }

    [Range(0, 120)]
    public int? MaxAge { get; init; }

    [MaxLength(500)]
    public string ImageUrl { get; init; }

    public IList<SectionInputViewModel> Sections { get; init; } = new List<SectionInputViewModel>();

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
}
