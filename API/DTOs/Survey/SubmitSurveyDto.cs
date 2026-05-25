using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.API.DTOs.Survey;

public record SubmitSurveyDto
{
    [Required] public IEnumerable<CreateResponseDto> Answers { get; init; }
}
