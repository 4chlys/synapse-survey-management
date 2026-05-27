using System.ComponentModel.DataAnnotations;
using SynapseEnqueteManagement.UI.Admin.Models.Enums;

namespace SynapseEnqueteManagement.UI.Admin.Models.Question;

public record EditQuestionViewModel
{
    public int QuestionId { get; init; }

    [Required]
    [MaxLength(500)]
    public string Text { get; init; }

    public QuestionFormat QuestionFormat { get; init; }

    public bool IsMandatory { get; init; }

    public string AnswerChoicesRaw { get; init; }

    public int? SectionId { get; init; }

    public string MediaUrl { get; init; }
    public QuestionMediaType? MediaType { get; init; }
    public bool MediaIsEmbed { get; init; }
    [MaxLength(2000)] public string MediaText { get; init; }

    public List<string> GetAnswerChoices() =>
        (AnswerChoicesRaw ?? string.Empty).Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();
}
