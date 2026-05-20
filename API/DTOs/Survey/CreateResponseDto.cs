using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.API.DTOs.Survey;

public record CreateResponseDto
{
    [MaxLength(500)] public string Value { get; init; }

    [Range(1, int.MaxValue)] public int QuestionId { get; init; }
}
