namespace SynapseEnqueteManagement.UI.Admin.Models.Category;

public class CategoryViewModel
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int IdeaCount { get; set; }
}
