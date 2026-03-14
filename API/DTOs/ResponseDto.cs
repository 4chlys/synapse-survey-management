namespace SynapseEnqueteManagement.API.DTOs;

public class ResponseDto
{
    public int ResponseId { get; set; }
    public string Value { get; set; }
    public int QuestionId { get; set; }
    public int ParticipantId { get; set; }
}