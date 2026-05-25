namespace SynapseEnqueteManagement.UI.User.Models.Ideation;

public record EmojiReactionViewModel
{
    public int EmojiId { get; init; }
    public string Symbol { get; init; }
    public int Count { get; init; }
    public bool PostedByCurrentUser { get; init; }
}
