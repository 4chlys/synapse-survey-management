using System.ComponentModel.DataAnnotations;
using SynapseEnqueteManagement.BL.Domain.I18n.Entities;

namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

public class Participant : IHasCreatedAt
{
    public int ParticipantId { get; set; }
    public DateTime CreatedAt { get; set; }
    [Required] public Guid SessionToken { get; set; } = Guid.NewGuid();
    public bool HasSubmitted { get; set; }
    public DateTime? CompletedAt { get; set; }
    [EmailAddress] [MaxLength(200)] public string Email { get; set; }
    public Language Language { get; set; }
    [Required] public Project Project { get; set; }
    [Required] public Questionnaire Questionnaire { get; set; }

    public ICollection<Idea> Ideas { get; set; } = new List<Idea>();
    public ICollection<Response> Responses { get; set; } = new List<Response>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}