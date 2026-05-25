using System.ComponentModel.DataAnnotations;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

public class TextComment : Comment
{
    [Required] [MaxLength(500)] public string Text { get; set; }
    public ModerationStatus ModerationStatus { get; set; }
    public ICollection<IconComment> EmojiReactions { get; set; } = new List<IconComment>();
}