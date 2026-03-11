using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

public class Idea
{
    public int IdeaId { get; set; }
    public ModerationStatus Status { get; set; }
    public string Description { get; set; }
    public int CommentCount { get; set; }

    public Category Category { get; set; }

    public Participant Participant { get; set; }
    public int? ParticipantId { get; set; }
    
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();

}