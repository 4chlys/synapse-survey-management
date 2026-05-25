namespace SynapseEnqueteManagement.API.DTOs.Stats.QuestionStats;

public record LikertPointCountDto
{
    public int Point { get; init; }
    public int Count { get; init; }
}
