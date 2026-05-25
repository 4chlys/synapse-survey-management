namespace SynapseEnqueteManagement.UI.User.Models.Ideation;

public record CategoryIndexViewModel
{
    public int ProjectId { get; init; }
    public ICollection<CategoryViewModel> Categories { get; init; } = new List<CategoryViewModel>();
}
