namespace SynapseEnqueteManagement.UI.User.Models;

public class CommentEmojiModel
{
    public int EmojiId { get; set; }
    public string Symbol { get; set; }
    public int Count { get; set; }
    public bool PostedByCurrentUser { get; set; }
}