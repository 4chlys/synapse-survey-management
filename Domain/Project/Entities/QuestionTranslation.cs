using System.ComponentModel.DataAnnotations;
using SynapseEnqueteManagement.BL.Domain.I18n.Entities;

namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

public class QuestionTranslation
{
    [Required] public Question Question { get; set; }
    [Required] public Language Language { get; set; }
    [Required] [MaxLength(500)] public string Text { get; set; }
    public ICollection<string> AnswerChoices { get; set; } = new List<string>();
    [MaxLength(2000)] public string MediaText { get; set; }
}
