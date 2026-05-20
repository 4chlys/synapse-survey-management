namespace SynapseEnqueteManagement.API.DTOs.Ideation;

public record IconCommentDto
{
    public int EmojiId { get; init; }
    public string Symbol { get; init; }
    public int Count { get; init; }
    public bool PostedByCurrentUser { get; init; }
}
