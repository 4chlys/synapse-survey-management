namespace SynapseEnqueteManagement.API.DTOs.Stats.QuestionStats;

public record LikertStatsDto
{
    public int QuestionId { get; init; }
    public int TotalResponses { get; init; }
    public ICollection<LikertPointCountDto> Distribution { get; init; } = new List<LikertPointCountDto>();
}
