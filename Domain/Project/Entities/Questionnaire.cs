namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

public class Questionnaire
{
    public int QuestionnaireId { get; set; }

    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<Participant> Participants { get; set; } = new List<Participant>();
}