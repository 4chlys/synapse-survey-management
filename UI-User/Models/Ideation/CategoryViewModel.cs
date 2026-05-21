namespace SynapseEnqueteManagement.UI.User.Models.Ideation;

public record CategoryViewModel
{
    public int CategoryId { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
    public int IdeaCount { get; init; }
}
