namespace SynapseEnqueteManagement.API.DTOs.Stats.QuestionStats;

public record QuestionLookupDto
{
    public int QuestionId { get; init; }
    public string Text { get; init; } = string.Empty;
}
