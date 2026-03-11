namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

// Emojis on comments
public class CommentEmoji
{
    public int CommentEmojiId { get; set; }
    public int CommentId { get; set; }
    public Comment Comment { get; set; }
    public int EmojiId { get; set; }
    public Emoji Emoji { get; set; }
}