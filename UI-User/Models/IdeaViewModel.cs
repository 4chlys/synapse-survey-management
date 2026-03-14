namespace SynapseEnqueteManagement.UI.User.Models;

public class IdeaViewModel
{
    public int IdeaId { get; set; }
    public string Description { get; set; }
    public string Category { get; set; }
    public int EmojiCount { get; set; }
    public int CommentsCount { get; set; }
    public bool IsOwnedByCurrentUser { get; set; }
    public bool IsFlagged { get; set; }
    public List<IdeaEmojiViewModel> Emojis { get; set; } = new();
}