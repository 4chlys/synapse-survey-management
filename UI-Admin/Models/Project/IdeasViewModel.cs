namespace SynapseEnqueteManagement.UI.Admin.Models.Project;

public class IdeasViewModel
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int TotalCount { get; set; }
    public int PageSize { get; set; } = 20;
    public int ApprovedCount { get; set; }
    public int PendingCount { get; set; }
    public int FlaggedCount { get; set; }
    public List<IdeaRowViewModel> Ideas { get; set; } = new();
    public List<CategoryOptionViewModel> Categories { get; set; } = new();
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public class IdeaRowViewModel
{
    public int IdeaId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int ReactionCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CategoryOptionViewModel
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
}
