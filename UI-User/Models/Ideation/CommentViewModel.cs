namespace SynapseEnqueteManagement.UI.User.Models.Ideation;

public record CommentViewModel
{
    public int CommentId { get; init; }
    public string Text { get; init; }
    public bool IsOwnedByCurrentUser { get; init; }
    public bool IsPostedByIdeaAuthor { get; init; }
    public bool IsFlagged { get; init; }
    public ICollection<EmojiReactionViewModel> Emojis { get; init; } = new List<EmojiReactionViewModel>();
}
