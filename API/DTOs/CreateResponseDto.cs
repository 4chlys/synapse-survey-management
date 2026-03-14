using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.API.DTOs;

public class CreateResponseDto
{
    [Required] [StringLength(500)] public string Value { get; set; }

    [Required] public int QuestionId { get; set; }
}