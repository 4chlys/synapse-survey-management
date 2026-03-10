using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

public class Project
{
    public int ProjectId { get; set; }
    
    public string OrganisationName { get; set; }
    public string OrganisationLogoUrl { get; set; }
    public string OrganisationWebsiteUrl { get; set; }
    
    public ProjectState ProjectState { get; set; } = ProjectState.Draft;

    public Questionnaire Questionnaire { get; set; }

    public Introduction Introduction { get; set; }

    public ICollection<Idea> Ideas { get; set; } = new List<Idea>();
    public ICollection<Participant> Participants { get; set; } = new List<Participant>();
}