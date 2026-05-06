using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.API.DTOs.Participants;

public class SetParticipantLanguageDto
{
    [Required]
    [MaxLength(10)]
    public string LanguageCode { get; set; }
}
