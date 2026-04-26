using SynapseEnqueteManagement.UI.Admin.Services.Dashboard;

namespace SynapseEnqueteManagement.UI.Admin.Models.Dashboard;

public class DashboardViewModel
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; }
    public int ViewCount { get; set; }
    public int CompletionCount { get; set; }
    public double CompletionRate { get; set; }
    public string SurveyUrl { get; set; }
    public List<CategoryMetricViewModel> TopCategories { get; set; } = new();
    public List<IdeaMetricViewModel> TopIdeas { get; set; } = new();
    public string[] WidgetLayout { get; set; } = WidgetPreferenceService.DefaultLayout;
    public string ApiBaseUrl { get; set; } = string.Empty;
}

public class CategoryMetricViewModel
{
    public int CategoryId { get; set; }
    public string Name { get; set; }
    public int IdeaCount { get; set; }
    public int TotalReactions { get; set; }
}

public class IdeaMetricViewModel
{
    public int IdeaId { get; set; }
    public string Description { get; set; }
    public int ReactionCount { get; set; }
    public int CommentCount { get; set; }
}
