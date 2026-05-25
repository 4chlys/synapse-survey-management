namespace SynapseEnqueteManagement.API.DTOs.Ideation;

public record CreatedIconCommentDto
{
    public int CommentId { get; init; }
    public int EmojiId { get; init; }
}
