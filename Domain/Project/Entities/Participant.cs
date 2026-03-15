using SynapseEnqueteManagement.BL.Domain;

namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

public class Participant : IHasCreatedAt
{
    public int ParticipantId { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid SessionToken { get; set; } = Guid.NewGuid();
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public Project Project { get; set; }

    public Questionnaire Questionnaire { get; set; }

    public ICollection<Idea> Ideas { get; set; } = new List<Idea>();
    public ICollection<Response> Responses { get; set; } = new List<Response>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}