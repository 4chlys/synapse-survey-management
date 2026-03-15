namespace SynapseEnqueteManagement.API.DTOs.Ideation;

public class TextCommentDto
{
    public int CommentId { get; set; }
    public string Text { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsOwnedByCurrentUser { get; set; }
    public bool IsFlagged { get; set; }
    public List<IconCommentDto> Emojis { get; set; } = new();
}
