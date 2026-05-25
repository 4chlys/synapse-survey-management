namespace SynapseEnqueteManagement.UI.Admin.Models.Category;

public record CategoryViewModel
{
    public int CategoryId { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
    public int IdeaCount { get; init; }
}
