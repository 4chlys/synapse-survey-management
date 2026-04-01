using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.UI.Admin.Models.Question;

public class CreateQuestionViewModel
{
    [Required(ErrorMessage = "Tekst is verplicht.")]
    [MaxLength(500)]
    public string Text { get; set; }

    [Required(ErrorMessage = "Kies een vraagtype.")]
    public string QuestionFormat { get; set; }

    public bool IsMandatory { get; set; }

    public string AnswerChoicesRaw { get; set; } = string.Empty;

    public List<string> GetAnswerChoices() =>
        AnswerChoicesRaw.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();
}