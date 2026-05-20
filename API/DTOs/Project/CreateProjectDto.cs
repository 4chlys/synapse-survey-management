using System.ComponentModel.DataAnnotations;
namespace SynapseEnqueteManagement.API.DTOs.Project;

public record CreateProjectDto
{
    [MaxLength(30)]
    [RegularExpression(@"^[a-z0-9](-?[a-z0-9])*$",
        ErrorMessage = "ShortForm must be lowercase alphanumeric with single hyphens.")]
    public string ShortForm { get; init; }

    [MaxLength(500)]
    public string ImageUrl { get; init; }

    [MaxLength(20)]
    public string InteractionMode { get; init; }

    [MaxLength(20)]
    public string AiAssistance { get; init; }

    [Range(0, 120)] public int? MinAge { get; init; }
    [Range(0, 120)] public int? MaxAge { get; init; }

    public bool RequestContactInfo { get; init; }

    [Range(1, 100)] public int IdeaPageSize { get; init; } = 5;
    public bool ExtraIdeasReplaceExisting { get; init; } = false;
    [Range(0, 50)] public int MaxExtraIdeaRequests { get; init; } = 3;
    public bool AllowCrossLanguageIdeas { get; init; } = false;

    [Range(1, int.MaxValue)]
    public int PlatformId { get; init; }

    [Required] [MaxLength(10)] public string DefaultLanguageCode { get; init; }
    [Required] [MinLength(1)] public ICollection<string> SupportedLanguageCodes { get; init; } = new List<string>();
    [Required] [MinLength(1)] public ICollection<ProjectTranslationDto> Translations { get; init; } = new List<ProjectTranslationDto>();

    [MaxLength(20)] public string Theme { get; init; } = "Default";

    [MaxLength(7)]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$",
        ErrorMessage = "Invalid color code. Use a hex value (e.g. #FF5733).")]
    public string PrimaryColor { get; init; }

    [MaxLength(4000)] public string CustomCss { get; init; }
}
