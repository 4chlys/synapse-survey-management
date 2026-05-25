namespace SynapseEnqueteManagement.UI.Admin.Models.Project;

public record IdeasViewModel
{
    public int ProjectId { get; init; }
    public string ProjectName { get; init; } = string.Empty;
    public string Search { get; init; }
    public int CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public string Status { get; init; }
    public int Page { get; init; } = 1;
    public int TotalCount { get; init; }
    public int PageSize { get; init; } = 20;
    public int ApprovedCount { get; init; }
    public int PendingCount { get; init; }
    public int FlaggedCount { get; init; }
    public bool RequestContactInfo { get; init; }
    public ICollection<IdeaRowViewModel> Ideas { get; init; } = new List<IdeaRowViewModel>();
    public ICollection<CategoryOptionViewModel> Categories { get; init; } = new List<CategoryOptionViewModel>();
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public record IdeaRowViewModel
{
    public int IdeaId { get; init; }
    public string Description { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public int CategoryId { get; init; }
    public string Status { get; init; } = string.Empty;
    public int ReactionCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public string ParticipantEmail { get; init; }
    public string ImageUrl { get; init; }
}

public record CategoryOptionViewModel
{
    public int CategoryId { get; init; }
    public string Name { get; init; } = string.Empty;
}
