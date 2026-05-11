namespace SynapseEnqueteManagement.API.DTOs.Survey;

public class EndingAnswerDto
{
    public int EndingAnswerId { get; set; }
    public ICollection<EndingAnswerTranslationDto> Translations { get; set; } = new List<EndingAnswerTranslationDto>();
}
