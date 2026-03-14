using SynapseEnqueteManagement.BL.Domain;

namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

public abstract class Comment : IHasCreatedAt
{
    public int CommentId { get; set; }
    public DateTime CreatedAt { get; set; }
    public Idea Idea { get; set; }
    public Participant Participant { get; set; }
}