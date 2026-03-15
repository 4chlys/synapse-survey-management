namespace SynapseEnqueteManagement.UI.User.Models.Ideation;

public class CommentViewModel
{
    public int CommentId { get; set; }
    public string Text { get; set; }
    public bool IsOwnedByCurrentUser { get; set; }
    public bool IsFlagged { get; set; }
    public List<CommentEmojiViewModel> Emojis { get; set; } = new();
}
