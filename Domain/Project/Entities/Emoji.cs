using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

public class Emoji
{
    public int EmojiId { get; set; }

    [Required]
    [MaxLength(10)]
    public string Symbol { get; set; }

    [Required]
    [MaxLength(20)]
    public string Name { get; set; }
    public bool IsActive { get; set; } = true; // Admin turns certain emojis on/off. to implement later...

   
}