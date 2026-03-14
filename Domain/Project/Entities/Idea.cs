using System.ComponentModel.DataAnnotations;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

public class Idea
{
    public int IdeaId { get; set; }
    public ModerationStatus Status { get; set; }

    [Required] [MaxLength(1000)] public string Description { get; set; }

    public Category Category { get; set; }

    public Participant Participant { get; set; }

    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}