namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

public class IconComment : Comment
{
    public int EmojiId { get; set; }
    public Emoji Emoji { get; set; }
}