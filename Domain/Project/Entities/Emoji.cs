namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

public class Emoji
{
    public int EmojiId { get; set; }
    public string Symbol { get; set; }
    public string Name { get; set; }
    public bool IsActive { get; set; } = true; // Admin turns certain emojis on/off. to implement later...
    public ICollection<EmojiReaction> EmojiReactions { get; set; } = new List<EmojiReaction>();

   
}