namespace SynapseEnqueteManagement.API.DTOs.Ideas;

public record AdminIdeaDetailDto
{
    public int IdeaId { get; init; }
    public string Description { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public string ParticipantEmail { get; init; }
    public string ImageUrl { get; init; }
    public string LanguageCode { get; init; }
    public IList<AdminIdeaEmojiReactionDto> EmojiReactions { get; init; } = new List<AdminIdeaEmojiReactionDto>();
    public IList<AdminCommentDto> Comments { get; init; } = new List<AdminCommentDto>();
}

public record AdminIdeaEmojiReactionDto
{
    public string Symbol { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int Count { get; init; }
}

public record AdminCommentDto
{
    public int CommentId { get; init; }
    public string Text { get; init; } = string.Empty;
    public string ModerationStatus { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public string ParticipantEmail { get; init; }
    public string LanguageCode { get; init; }
    public IList<AdminIdeaEmojiReactionDto> EmojiReactions { get; init; } = new List<AdminIdeaEmojiReactionDto>();
}
