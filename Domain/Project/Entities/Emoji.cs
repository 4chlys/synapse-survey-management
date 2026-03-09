namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

public class EmojiReaction
{
    public int EmojiReactionId { get; set; }
    public string Emoji { get; set; }
    public Idea Idea { get; set; }
    public Participant Participant { get; set; }
}