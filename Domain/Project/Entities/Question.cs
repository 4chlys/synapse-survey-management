using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

public class Question(QuestionFormat questionFormat, Questionnaire questionnaire, Section section)
{
    public QuestionFormat QuestionFormat { get; set; } = questionFormat;
    public ICollection<string> AnswerChoices { get; set; } = new List<string>();
    
    
    public Questionnaire Questionnaire { get; set; } = questionnaire;
    public Section Section { get; set; } = section;
    public ICollection<Response> Responses { get; set; }
}