using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.API.DTOs.Survey;

public class CreateResponseDto
{
    [MaxLength(500)] public string Value { get; set; }

    [Range(1, int.MaxValue)] public int QuestionId { get; set; }
}
