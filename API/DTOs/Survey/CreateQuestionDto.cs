using System.ComponentModel.DataAnnotations;
namespace SynapseEnqueteManagement.API.DTOs.Survey;

public class CreateQuestionDto
{
    [Required][MaxLength(500)]
    public string Text { get; set; }

    [Required]
    public string QuestionFormat { get; set; }

    public bool IsMandatory { get; set; }

    public ICollection<string> AnswerChoices { get; set; } = new List<string>();

    public int? SectionId { get; set; }

    public int? ParentQuestionId { get; set; }

    public string ParentTriggerValue { get; set; }

    public ICollection<string> EndingAnswers { get; set; } = new List<string>();
}
