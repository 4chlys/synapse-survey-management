namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

public class Participant(Questionnaire questionnaire)
{
    public  Questionnaire Questionnaire { get; set; } = questionnaire;
    public ICollection<Idea> Ideas { get; set; } = new  List<Idea>();
    public ICollection<Response> Responses { get; set; }
    public ICollection<Comment> Comments { get; set; }
}