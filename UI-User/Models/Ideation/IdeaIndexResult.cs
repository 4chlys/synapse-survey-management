namespace SynapseEnqueteManagement.UI.User.Models.Ideation;

public sealed class IdeaIndexResult
{
    public List<IdeaViewModel> Ideas { get; set; } = new();
    public string ResolvedSort { get; set; } = "diverse";
    public bool HasOwnIdeas { get; set; }
}
