namespace SynapseEnqueteManagement.API.DTOs;

public class IdeaDto
{
    public int IdeaId { get; set; }
    public string Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CommentsCount { get; set; }
    public bool IsOwnedByCurrentUser { get; set; }
    public bool IsFlagged { get; set; }
    public List<IconCommentDto> Emojis { get; set; } = new();
}