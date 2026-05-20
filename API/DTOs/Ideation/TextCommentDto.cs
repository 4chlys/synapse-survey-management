namespace SynapseEnqueteManagement.API.DTOs.Ideation;

public class TextCommentDto
{
    public int CommentId { get; set; }
    public string Text { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsOwnedByCurrentUser { get; set; }
    public bool IsPostedByIdeaAuthor { get; set; }
    public bool IsFlagged { get; set; }
    public string LanguageCode { get; set; }
    public ICollection<IconCommentDto> Emojis { get; set; } = new List<IconCommentDto>();
}
