namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

public class Project(Questionnaire questionnaire, Introduction introduction)
{
    public Questionnaire Questionnaire { get; set; } = questionnaire;
    public Introduction Introduction { get; set; } = introduction;
    public ICollection<Idea> Ideas { get; set; }
    public ICollection<Participant> Participants { get; set; }
}