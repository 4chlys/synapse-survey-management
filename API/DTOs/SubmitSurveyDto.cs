using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.API.DTOs;

public class SubmitSurveyDto
{
    [Required] public IEnumerable<CreateResponseDto> Answers { get; set; }
}