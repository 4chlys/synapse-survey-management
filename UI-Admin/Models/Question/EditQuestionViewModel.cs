using System.ComponentModel.DataAnnotations;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.UI.Admin.Models.Enums;
using QuestionFormat = SynapseEnqueteManagement.UI.Admin.Models.Enums.QuestionFormat;

namespace SynapseEnqueteManagement.UI.Admin.Models.Question;

public class EditQuestionViewModel
{
    public int QuestionId { get; set; }

    [Required]
    [MaxLength(500)]
    public string Text { get; set; }

    public QuestionFormat QuestionFormat { get; set; }

    public bool IsMandatory { get; set; }

    public string AnswerChoicesRaw { get; set; }

    public int? SectionId { get; set; }

    public string MediaUrl { get; set; }
    public QuestionMediaType? MediaType { get; set; }
    public bool MediaIsEmbed { get; set; }
    [MaxLength(2000)] public string MediaText { get; set; }

    public List<string> GetAnswerChoices() =>
        (AnswerChoicesRaw ?? string.Empty).Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();
}