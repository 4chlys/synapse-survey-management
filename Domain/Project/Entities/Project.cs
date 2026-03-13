using System.ComponentModel.DataAnnotations;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

public class Project
{
    public int ProjectId { get; set; }

    [Required]
    [MaxLength(50)]
    public string OrganisationName { get; set; }

    [MaxLength(100)]
    [RegularExpression(@"^https?://.*", ErrorMessage = "Only http and https URLs are allowed.")]
    public string OrganisationLogoUrl { get; set; }

    [MaxLength(50)]
    [RegularExpression(@"^https?://.*", ErrorMessage = "Only http and https URLs are allowed.")]
    public string OrganisationWebsiteUrl { get; set; }
    
    public ProjectState ProjectState { get; set; } = ProjectState.Draft;
    public FormType FormType { get; set; } = FormType.VerticalScroll;

    public Questionnaire Questionnaire { get; set; }

    public Introduction Introduction { get; set; }

    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<Participant> Participants { get; set; } = new List<Participant>();
}