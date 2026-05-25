namespace SynapseEnqueteManagement.API.DTOs.Ideation;

public record IdeaDto
{
    public int IdeaId { get; init; }
    public string Description { get; init; }
    public string ImageUrl { get; init; }
    public DateTime CreatedAt { get; init; }
    public int CommentsCount { get; init; }
    public bool IsOwnedByCurrentUser { get; init; }
    public bool IsFlagged { get; init; }
    public string LanguageCode { get; init; }
    public ICollection<IconCommentDto> Emojis { get; init; } = new List<IconCommentDto>();
}
