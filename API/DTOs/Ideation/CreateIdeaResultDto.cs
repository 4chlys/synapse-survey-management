namespace SynapseEnqueteManagement.API.DTOs.Ideation;

public record CreateIdeaResultDto
{
    public int IdeaId { get; init; }
    public bool ShouldAskForContactInfo { get; init; }
}
