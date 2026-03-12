using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

public class TextComment : Comment
{
    public string Text { get; set; }
    public ModerationStatus ModerationStatus { get; set; }
    public ICollection<IconComment> Reactions { get; set; } = new List<IconComment>();
    
}