using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.API.DTOs.Ideation;

public class AddIconCommentDto
{
    [Range(1, int.MaxValue)] public int EmojiId { get; set; }
}
