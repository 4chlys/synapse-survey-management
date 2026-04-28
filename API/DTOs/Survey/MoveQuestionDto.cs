using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.API.DTOs.Survey;

public class MoveQuestionDto
{
    public int? ParentQuestionId { get; set; }

    [MaxLength(500)]
    public string ParentTriggerValue { get; set; }

    [Range(1, int.MaxValue)]
    public int Position { get; set; }
}
