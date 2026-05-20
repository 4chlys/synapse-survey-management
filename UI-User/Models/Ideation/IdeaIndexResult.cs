namespace SynapseEnqueteManagement.UI.User.Models.Ideation;

public sealed record IdeaIndexResult
{
    public List<IdeaViewModel> Ideas { get; init; } = new();
    public string ResolvedSort { get; init; } = "diverse";
    public bool HasOwnIdeas { get; init; }
}
