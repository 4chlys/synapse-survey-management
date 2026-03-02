namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

public class IconComment(Idea idea, Emoji emoji) : Comment(idea)
{
    public Emoji Emoji { get; set; } = emoji;
}