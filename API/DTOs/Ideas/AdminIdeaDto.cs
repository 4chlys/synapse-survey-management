namespace SynapseEnqueteManagement.API.DTOs.Ideas;

public class AdminIdeaDto
{
    public int IdeaId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int ReactionCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public string ParticipantEmail { get; set; }
}
