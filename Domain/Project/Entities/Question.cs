using System.ComponentModel.DataAnnotations;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

public class Question
{
    public int QuestionId { get; set; }
    public QuestionFormat QuestionFormat { get; set; }
    public bool IsMandatory { get; set; }
    public int AnswerChoiceCount { get; set; }
    public int? ChoiceIndex { get; set; }

    [Required] public Questionnaire Questionnaire { get; set; }
    public Section Section { get; set; }
    [Range(1, int.MaxValue)] public int Position { get; set; }
    public Question ParentQuestion { get; set; }
    public ICollection<Question> Children { get; set; } = new List<Question>();
    public ICollection<EndingAnswer> EndingAnswers { get; set; } = new List<EndingAnswer>();
    public ICollection<Response> Responses { get; set; } = new List<Response>();
    public ICollection<QuestionTranslation> Translations { get; set; } = new List<QuestionTranslation>();

    [MaxLength(500)] public string MediaUrl { get; set; }
    public QuestionMediaType? MediaType { get; set; }
    public bool MediaIsEmbed { get; set; }
}
