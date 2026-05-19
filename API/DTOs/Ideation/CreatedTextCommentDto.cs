namespace SynapseEnqueteManagement.API.DTOs.Ideation;

public record CreatedTextCommentDto
{
    public int CommentId { get; init; }
    public string Text { get; init; }
}
