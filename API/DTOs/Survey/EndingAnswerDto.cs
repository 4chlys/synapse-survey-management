namespace SynapseEnqueteManagement.API.DTOs.Survey;

public record EndingAnswerDto
{
    public int EndingAnswerId { get; init; }
    public ICollection<EndingAnswerTranslationDto> Translations { get; init; } = new List<EndingAnswerTranslationDto>();
}
