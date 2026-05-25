namespace SynapseEnqueteManagement.API.DTOs.Ideation;

public record TextCommentDto
{
    public int CommentId { get; init; }
    public string Text { get; init; }
    public DateTime CreatedAt { get; init; }
    public bool IsOwnedByCurrentUser { get; init; }
    public bool IsPostedByIdeaAuthor { get; init; }
    public bool IsFlagged { get; init; }
    public string LanguageCode { get; init; }
    public ICollection<IconCommentDto> Emojis { get; init; } = new List<IconCommentDto>();
}
