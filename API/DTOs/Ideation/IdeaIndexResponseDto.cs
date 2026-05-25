using SynapseEnqueteManagement.BL;

namespace SynapseEnqueteManagement.API.DTOs.Ideation;

public record IdeaIndexResponseDto : PagedResult<IdeaDto>
{
    public string ResolvedSort { get; init; }
    public bool HasOwnIdeas { get; init; }
}
