using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

public class Question
{
    public int QuestionId { get; set; }
    
    public String Text { get; set; }
    public QuestionFormat QuestionFormat { get; set; }
    public ICollection<string> AnswerChoices { get; set; } = new List<string>();

    public Questionnaire Questionnaire { get; set; }

    public Section Section { get; set; }

    public ICollection<Response> Responses { get; set; } = new List<Response>();
}