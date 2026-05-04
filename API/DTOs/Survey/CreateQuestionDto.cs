using System.ComponentModel.DataAnnotations;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.API.DTOs.Survey;

public class CreateQuestionDto
{
    [Required] [MaxLength(500)]
    public string Text { get; set; }

    [Required] [MaxLength(50)]
    public string QuestionFormat { get; set; }

    public bool IsMandatory { get; set; }

    public ICollection<string> AnswerChoices { get; set; } = new List<string>();

    public int? SectionId { get; set; }

    public int? ParentQuestionId { get; set; }

    [MaxLength(500)]
    public string ParentTriggerValue { get; set; }

    public ICollection<string> EndingAnswers { get; set; } = new List<string>();

    [MaxLength(500)]
    public string MediaUrl { get; set; }

    public QuestionMediaType? MediaType { get; set; }

    public bool MediaIsEmbed { get; set; }

    [MaxLength(2000)]
    public string MediaText { get; set; }
}
