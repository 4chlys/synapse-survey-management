namespace SynapseEnqueteManagement.API.DTOs.Ideation;

public class IdeaDto
{
    public int IdeaId { get; set; }
    public string Description { get; set; }
    public string ImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CommentsCount { get; set; }
    public bool IsOwnedByCurrentUser { get; set; }
    public bool IsFlagged { get; set; }
    public string LanguageCode { get; set; }
    public ICollection<IconCommentDto> Emojis { get; set; } = new List<IconCommentDto>();
}
