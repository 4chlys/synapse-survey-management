using System.ComponentModel.DataAnnotations;
using SynapseEnqueteManagement.UI.Admin.Models.Enums;

namespace SynapseEnqueteManagement.UI.Admin.Models.Question;

public class CreateQuestionViewModel
{
    [Required]
    [MaxLength(500)]
    public string Text { get; set; }
    
    public QuestionFormat QuestionFormat { get; set; }

    public bool IsMandatory { get; set; }

    public string AnswerChoicesRaw { get; set; }

    public int? SectionId { get; set; }

    public List<string> GetAnswerChoices() =>
        (AnswerChoicesRaw ?? string.Empty).Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();
}