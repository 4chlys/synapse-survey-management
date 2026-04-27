using System.ComponentModel.DataAnnotations;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.BL.Domain;

namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

public class Idea : IHasCreatedAt
{
    public int IdeaId { get; set; }
    [Required] public ModerationStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }

    [Required] [MaxLength(1000)] public string Description { get; set; }

    // Vector embedding of Description used by embedding-based sort strategies
    // must skip or cold-generate such entries. DAL maps to pgvector or JSON.
    public float[] Embedding { get; set; }

    [Required] public Category Category { get; set; }

    [Required] public Participant Participant { get; set; }

    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}