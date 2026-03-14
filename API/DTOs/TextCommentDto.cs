namespace SynapseEnqueteManagement.API.DTOs;

public class TextCommentDto
{
    public int CommentId { get; set; }
    public string Text { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<IconCommentDto> Emojis { get; set; } = new();
}