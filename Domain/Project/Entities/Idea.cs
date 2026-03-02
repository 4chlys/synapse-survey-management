using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

public class Idea(Category category, ModerationStatus status, Participant participant)
{
    public ModerationStatus Status { get; set; } = status;
    
    public Category Category { get; set; } = category;
    public Participant Participant { get; set; } = participant;
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}