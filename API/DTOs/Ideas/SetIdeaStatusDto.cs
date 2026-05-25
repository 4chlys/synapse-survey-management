namespace SynapseEnqueteManagement.API.DTOs.Ideas;

public record SetIdeaStatusDto
{
    public string Status { get; init; } = string.Empty;
}
