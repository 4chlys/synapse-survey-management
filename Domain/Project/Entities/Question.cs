using System.ComponentModel.DataAnnotations;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

public class Question
{
    public int QuestionId { get; set; }

    [Required] [MaxLength(500)] public string Text { get; set; }
    public QuestionFormat QuestionFormat { get; set; }
    public bool IsMandatory { get; set; }
    public ICollection<string> AnswerChoices { get; set; } = new List<string>();

    public Questionnaire Questionnaire { get; set; }

    public Section Section { get; set; }
    public int Position { get; set; }
    public ICollection<BranchRule> BranchRules { get; set; } = [];
    public ICollection<BranchRule> IncomingBranches { get; set; } = [];

    public ICollection<Response> Responses { get; set; } = new List<Response>();
}