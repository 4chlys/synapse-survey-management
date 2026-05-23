namespace SynapseEnqueteManagement.UI.User.Models.Ideation;

public record IdeaViewModel
{
    public int IdeaId { get; init; }
    public string Description { get; init; }
    public string ImageUrl { get; init; }
    public string Category { get; init; }
    public int EmojiCount { get; init; }
    public int CommentsCount { get; init; }
    public bool IsOwnedByCurrentUser { get; init; }
    public bool IsFlagged { get; init; }
    // Language the idea was written in; drives the per-element `lang` attribute for cross-language ideas.
    public string LanguageCode { get; init; }
    public ICollection<EmojiReactionViewModel> Emojis { get; init; } = new List<EmojiReactionViewModel>();
}
