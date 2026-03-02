namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

public class Project
{
    public int ProjectId { get; set; }

    public Questionnaire Questionnaire { get; set; }

    public Introduction Introduction { get; set; }

    public ICollection<Idea> Ideas { get; set; } = new List<Idea>();
    public ICollection<Participant> Participants { get; set; } = new List<Participant>();
    
}