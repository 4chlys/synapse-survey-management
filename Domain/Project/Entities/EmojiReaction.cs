namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

public class EmojiReaction
{
    public int EmojiReactionId { get; set; }
    public int EmojiId { get; set; }
    public Emoji Emoji { get; set; }
    public int IdeaId { get; set; }
    public Idea Idea { get; set; }
    public int ParticipantId { get; set; }
    public Participant Participant { get; set; }
}