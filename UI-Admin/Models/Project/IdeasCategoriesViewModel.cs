using SynapseEnqueteManagement.UI.Admin.Models.Category;

namespace SynapseEnqueteManagement.UI.Admin.Models.Project;

public record IdeasCategoriesViewModel
{
    public int ProjectId { get; init; }
    public string ProjectName { get; init; } = string.Empty;
    public int TotalIdeaCount { get; init; }
    public int TotalApproved { get; init; }
    public int TotalPending { get; init; }
    public int TotalFlagged { get; init; }
    public int TotalDeclined { get; init; }
    public ICollection<CategoryViewModel> Categories { get; init; } = new List<CategoryViewModel>();
}
