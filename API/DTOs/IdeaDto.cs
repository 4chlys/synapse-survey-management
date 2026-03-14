namespace SynapseEnqueteManagement.API.DTOs;

public class IdeaDto
{
    public int IdeaId { get; set; }
    public string Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CommentsCount { get; set; }
    public List<IdeaEmojiDto> Emojis { get; set; } = new();
}