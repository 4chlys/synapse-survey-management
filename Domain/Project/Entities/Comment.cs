using System.ComponentModel.DataAnnotations;
using SynapseEnqueteManagement.BL.Domain.I18n.Entities;

namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

public abstract class Comment : IHasCreatedAt
{
    public int CommentId { get; set; }
    public DateTime CreatedAt { get; set; }
    [Required] public Idea Idea { get; set; }
    [Required] public Participant Participant { get; set; }
    public Language Language { get; set; }
}