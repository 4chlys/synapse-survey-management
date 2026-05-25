using System.ComponentModel.DataAnnotations;
using SynapseEnqueteManagement.BL.Domain;
using SynapseEnqueteManagement.BL.Domain.I18n.Entities;
using SynapseEnqueteManagement.BL.Domain.Platform.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

public class Project : IHasCreatedAt
{
    public int ProjectId { get; set; }
    public DateTime CreatedAt { get; set; }

    public int ProjectPlatformId { get; set; }
    [Required] public ProjectPlatform ProjectPlatform { get; set; }
    public int ViewCount { get; set; } = 0;

    [Required]
    [MaxLength(30)]
    [RegularExpression(@"^[a-z0-9](-?[a-z0-9])*$",
        ErrorMessage = "ShortForm must be lowercase alphanumeric with single hyphens.")]
    public string ShortForm { get; set; }

    public Language DefaultLanguage { get; set; }
    public ICollection<Language> SupportedLanguages { get; set; } = new List<Language>();
    public ICollection<ProjectTranslation> Translations { get; set; } = new List<ProjectTranslation>();

    [Range(0, 120)] public int? MinAge { get; set; }
    [Range(0, 120)] public int? MaxAge { get; set; }

    [MaxLength(500)]
    public string ImageUrl { get; set; }

    public ProjectState ProjectState { get; set; } = ProjectState.Draft;
    public FormType FormType { get; set; } = FormType.VerticalScroll;

    public ProjectTheme Theme { get; set; } = ProjectTheme.Minimal;

    [MaxLength(7)]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$",
        ErrorMessage = "Invalid color code. Use a hex value (e.g. #FF5733).")]
    public string PrimaryColor { get; set; }

    [MaxLength(4000)]
    public string CustomCss { get; set; }

    [Range(1, 100)] public int IdeaPageSize { get; set; } = 5;
    public bool ExtraIdeasReplaceExisting { get; set; } = false;
    [Range(0, 50)] public int MaxExtraIdeaRequests { get; set; } = 3;
    [Range(1, 200)] public int CommentPageSize { get; set; } = 20;
    public bool RequestContactInfo { get; set; } = false;
    public bool AllowCrossLanguageIdeas { get; set; } = false;

    [Range(1, 3)] public int LastWizardStep { get; set; } = 1;

    public Questionnaire Questionnaire { get; set; }

    public Introduction Introduction { get; set; }

    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<Participant> Participants { get; set; } = new List<Participant>();
    public ICollection<Emoji> Emojis { get; set; } = new List<Emoji>();
}