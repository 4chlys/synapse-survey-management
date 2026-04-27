using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

public class IconComment : Comment
{
    public int EmojiId { get; set; }
    [Required] public Emoji Emoji { get; set; }
    public TextComment TextComment { get; set; }
}