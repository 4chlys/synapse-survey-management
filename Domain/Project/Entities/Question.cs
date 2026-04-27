using System.ComponentModel.DataAnnotations;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

public class Question
{
    public int QuestionId { get; set; }

    [Required] [MaxLength(500)] public string Text { get; set; }
    [Required] public QuestionFormat QuestionFormat { get; set; }
    public bool IsMandatory { get; set; }
    public ICollection<string> AnswerChoices { get; set; } = new List<string>();

    [Required] public Questionnaire Questionnaire { get; set; }

    public Section Section { get; set; }
    [Range(1, int.MaxValue)] public int Position { get; set; }

    public Question ParentQuestion { get; set; }

    [MaxLength(500)] public string ParentTriggerValue { get; set; }

    public ICollection<Question> Children { get; set; } = new List<Question>();

    public ICollection<EndingAnswer> EndingAnswers { get; set; } = new List<EndingAnswer>();

    public ICollection<Response> Responses { get; set; } = new List<Response>();
}
