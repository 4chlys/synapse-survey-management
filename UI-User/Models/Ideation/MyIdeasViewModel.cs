namespace SynapseEnqueteManagement.UI.User.Models.Ideation;

public record MyIdeasViewModel
{
    public int ProjectId { get; init; }
    public int CategoryId { get; init; }
    public CategoryViewModel Category { get; init; } = null!;
    public ICollection<IdeaViewModel> Ideas { get; init; } = new List<IdeaViewModel>();
}
