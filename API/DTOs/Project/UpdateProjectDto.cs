using System.ComponentModel.DataAnnotations;
namespace SynapseEnqueteManagement.API.DTOs.Project;

public class UpdateProjectDto
{
    [Required]
    [MaxLength(30)]
    [RegularExpression(@"^[a-z0-9](-?[a-z0-9])*$",
        ErrorMessage = "ShortForm must be lowercase alphanumeric with single hyphens.")]
    public string ShortForm { get; set; }

    [MaxLength(500)]
    public string ImageUrl { get; set; }

    [MaxLength(20)]
    public string InteractionMode { get; set; }

    [MaxLength(20)]
    public string AiAssistance { get; set; }

    [Range(0, 120)] public int? MinAge { get; set; }
    [Range(0, 120)] public int? MaxAge { get; set; }

    public bool RequestContactInfo { get; set; }

    [Range(1, 100)] public int IdeaPageSize { get; set; } = 5;
    public bool ExtraIdeasReplaceExisting { get; set; } = false;
    [Range(0, 50)] public int MaxExtraIdeaRequests { get; set; } = 3;
    public bool AllowCrossLanguageIdeas { get; set; } = false;

    [Required] [MaxLength(10)] public string DefaultLanguageCode { get; set; }
    [Required] [MinLength(1)] public ICollection<string> SupportedLanguageCodes { get; set; } = new List<string>();
    [Required] [MinLength(1)] public ICollection<ProjectTranslationDto> Translations { get; set; } = new List<ProjectTranslationDto>();
}
