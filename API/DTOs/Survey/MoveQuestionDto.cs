using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.API.DTOs.Survey;

public class MoveQuestionDto
{
    public int? ParentQuestionId { get; set; }
    public int? ChoiceIndex { get; set; }

    [Range(1, int.MaxValue)]
    public int Position { get; set; }
}
