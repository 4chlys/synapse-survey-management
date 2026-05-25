namespace SynapseEnqueteManagement.API.DTOs.Stats;

public record IdeaFeedItemDto
{
    public int IdeaId { get; init; }
    public string Description { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public int ReactionCount { get; init; }
    public DateTime CreatedAt { get; init; }
}
