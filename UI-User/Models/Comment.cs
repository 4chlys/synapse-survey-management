namespace SynapseEnqueteManagement.UI.User.Models;

public class Comment
{
    public int CommentId { get; set; }
    public string Text { get; set; }
    public List<CommentEmojiModel> Emojis { get; set; } = new();
}