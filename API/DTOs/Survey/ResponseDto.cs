namespace SynapseEnqueteManagement.API.DTOs.Survey;

public record ResponseDto
{
    public int ResponseId { get; init; }
    public string Value { get; init; }
    public int QuestionId { get; init; }
    public int ParticipantId { get; init; }
    public string LanguageCode { get; init; }
}
