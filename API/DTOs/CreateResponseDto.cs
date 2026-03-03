using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.API.DTOs;

public class CreateResponseDto
{
    [Required]
    public string Value { get; set; }

    [Required]
    public int QuestionId { get; set; }
}
