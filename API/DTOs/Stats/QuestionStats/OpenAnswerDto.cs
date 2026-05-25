namespace SynapseEnqueteManagement.API.DTOs.Stats.QuestionStats;

public record OpenAnswerDto
{
    public string Value { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
