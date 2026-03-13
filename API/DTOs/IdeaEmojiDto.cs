namespace SynapseEnqueteManagement.API.DTOs;

public class IdeaEmojiDto
{
    public int EmojiId { get; set; }
    public string Symbol { get; set; }
    public int Count { get; set; }
    public bool PostedByCurrentUser { get; set; }
}