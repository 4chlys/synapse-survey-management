namespace SynapseEnqueteManagement.API.DTOs.Ideas;

public record AdminIdeaDto
{
    public int IdeaId { get; init; }
    public string Description { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public int CategoryId { get; init; }
    public string Status { get; init; } = string.Empty;
    public int ReactionCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public string ParticipantEmail { get; init; }
    public string ImageUrl { get; init; }
    public string LanguageCode { get; init; }
}
