namespace SynapseEnqueteManagement.UI.User.Models.Ideation;

public class CommentViewModel
{
    public int CommentId { get; set; }
    public string Text { get; set; }
    public bool IsOwnedByCurrentUser { get; set; }
    public bool IsPostedByIdeaAuthor { get; set; }
    public bool IsFlagged { get; set; }
    // Language the comment was written in; drives the per-element `lang` attribute for cross-language comments.
    public string LanguageCode { get; set; }
    public List<EmojiReactionViewModel> Emojis { get; set; } = new();
}
