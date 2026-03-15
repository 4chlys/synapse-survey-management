namespace SynapseEnqueteManagement.UI.User.Models.Ideation;

public class CategoryIndexViewModel
{
    public int ProjectId { get; set; }
    public List<CategoryViewModel> Categories { get; set; } = new();
}
