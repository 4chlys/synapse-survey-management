using SynapseEnqueteManagement.UI.Admin.Services.Dashboard;

namespace SynapseEnqueteManagement.UI.Admin.Models.Dashboard;

public record DashboardViewModel
{
    public int ProjectId { get; init; }
    public string ProjectName { get; init; }
    public int ViewCount { get; init; }
    public int CompletionCount { get; init; }
    public double CompletionRate { get; init; }
    public string SurveyUrl { get; init; }
    public ICollection<CategoryMetricViewModel> TopCategories { get; init; } = new List<CategoryMetricViewModel>();
    public ICollection<IdeaMetricViewModel> TopIdeas { get; init; } = new List<IdeaMetricViewModel>();
    public string[] WidgetLayout { get; init; } = WidgetSlotValidator.DefaultLayout;
    public WidgetSlot[] WidgetSlots { get; init; } = Array.ConvertAll(
        WidgetSlotValidator.DefaultLayout, t => new WidgetSlot(t, null));
    public string ApiBaseUrl { get; init; } = string.Empty;
}

public record CategoryMetricViewModel
{
    public int CategoryId { get; init; }
    public string Name { get; init; }
    public int IdeaCount { get; init; }
    public int TotalReactions { get; init; }
}

public record IdeaMetricViewModel
{
    public int IdeaId { get; init; }
    public string Description { get; init; }
    public int ReactionCount { get; init; }
    public int CommentCount { get; init; }
}
