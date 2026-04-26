using System.ComponentModel.DataAnnotations;
using SynapseEnqueteManagement.BL.Domain;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

public class Project : IHasCreatedAt
{
    public int ProjectId { get; set; }
    public DateTime CreatedAt { get; set; }

    [Required] [MaxLength(50)] public string OrganisationName { get; set; }

    [MaxLength(100)]
    [RegularExpression(@"^https?://.*", ErrorMessage = "Only http and https URLs are allowed.")]
    public string OrganisationLogoUrl { get; set; }

    [MaxLength(50)]
    [RegularExpression(@"^https?://.*", ErrorMessage = "Only http and https URLs are allowed.")]
    public string OrganisationWebsiteUrl { get; set; }

    [Required] [MaxLength(100)]
    public string Name { get; set; }

    [Required]
    [MaxLength(30)]
    [RegularExpression(@"^[a-z0-9](-?[a-z0-9])*$",
        ErrorMessage = "ShortForm must be lowercase alphanumeric with single hyphens.")]
    public string ShortForm { get; set; }

    [MaxLength(100)]
    public string Topic { get; set; }

    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }

    [MaxLength(500)]
    public string ImageUrl { get; set; }

    public AiAssistanceType AiAssistance { get; set; } = AiAssistanceType.ProActive;

    public ProjectState ProjectState { get; set; } = ProjectState.Draft;
    public FormType FormType { get; set; } = FormType.VerticalScroll;
    public int IdeaPageSize { get; set; } = 5;
    public int CommentPageSize { get; set; } = 20;
    public bool RequestContactInfo { get; set; } = false;

    public Questionnaire Questionnaire { get; set; }

    public Introduction Introduction { get; set; }

    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<Participant> Participants { get; set; } = new List<Participant>();
    public ICollection<Emoji> Emojis { get; set; } = new List<Emoji>();
}