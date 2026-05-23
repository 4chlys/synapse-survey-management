using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.API.DTOs.Ideation;

public record AddIconCommentDto
{
    [Range(1, int.MaxValue)] public int EmojiId { get; init; }
}
