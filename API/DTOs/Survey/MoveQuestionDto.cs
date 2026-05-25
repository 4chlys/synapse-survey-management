using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.API.DTOs.Survey;

public record MoveQuestionDto
{
    public int? ParentQuestionId { get; init; }
    public int? ChoiceIndex { get; init; }

    [Range(1, int.MaxValue)]
    public int Position { get; init; }
}
