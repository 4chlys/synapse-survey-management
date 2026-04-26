using SynapseEnqueteManagement.BL;

namespace SynapseEnqueteManagement.API.DTOs.Ideation;

public class IdeaIndexResponseDto : PagedResult<IdeaDto>
{
    public string ResolvedSort { get; set; }
    public bool HasOwnIdeas { get; set; }
}
