namespace SynapseEnqueteManagement.API.DTOs;

public class IconCommentDto
{
    public int EmojiId { get; set; }
    public string Symbol { get; set; }
    public int Count { get; set; }
    public bool PostedByCurrentUser { get; set; }
}