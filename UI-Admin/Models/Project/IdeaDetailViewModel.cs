namespace SynapseEnqueteManagement.UI.Admin.Models.Project;

public record IdeaDetailViewModel
{
    public int ProjectId { get; init; }
    public int CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public int IdeaId { get; init; }
    public string Description { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public string ParticipantEmail { get; init; }
    public string ImageUrl { get; init; }
    public string LanguageCode { get; init; }
    public IList<IdeaEmojiReactionViewModel> EmojiReactions { get; init; } = new List<IdeaEmojiReactionViewModel>();
    public IList<IdeaCommentViewModel> Comments { get; init; } = new List<IdeaCommentViewModel>();

    public string BackUrl { get; init; } = string.Empty;
}

public record IdeaEmojiReactionViewModel
{
    public string Symbol { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int Count { get; init; }
}

public record IdeaCommentViewModel
{
    public int CommentId { get; init; }
    public string Text { get; init; } = string.Empty;
    public string ModerationStatus { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public string ParticipantEmail { get; init; }
    public string LanguageCode { get; init; }
    public IList<IdeaEmojiReactionViewModel> EmojiReactions { get; init; } = new List<IdeaEmojiReactionViewModel>();
}
