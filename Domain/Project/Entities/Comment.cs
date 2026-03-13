namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

public abstract class Comment
{
    public int CommentId { get; set; }
    public Idea Idea { get; set; }
    public Participant Participant { get; set; }
}