namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

// Emojis on Ideas
public class IconComment : Comment
{
    public int EmojiId { get; set; }
    public Emoji Emoji { get; set; }
}