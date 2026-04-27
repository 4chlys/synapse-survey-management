using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.BL;

// Paged ideas plus metadata the UI needs to render the index page:
//   ResolvedSort — the sort actually used (server may fall back).
//   HasOwnIdeas — whether the participant has any idea in this category;
//                 controls visibility of similar/different anchors in the UI.
public class IdeaIndexResult : PagedResult<Idea>
{
    public string ResolvedSort { get; set; }
    public bool HasOwnIdeas { get; set; }
}
