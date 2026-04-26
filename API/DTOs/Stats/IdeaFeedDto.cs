namespace SynapseEnqueteManagement.API.DTOs.Stats;

public class IdeaFeedItemDto
{
    public int IdeaId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int ReactionCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
