using Microsoft.EntityFrameworkCore;
using SynapseEnqueteManagement.UI.Admin.Data;

namespace SynapseEnqueteManagement.UI.Admin.Services.Dashboard;

public class WidgetPreferenceService(AdminDbContext db)
{
    public static readonly string[] DefaultLayout =
    [
        "ViewCount", "CompletionCount", "CompletionRate",
        "ParticipationTimeline", "TopIdeas", "TopCategories",
        "SurveyDistribution", "IdeaFeed", "ModerationQueue"
    ];

    public static readonly string[] AllWidgetTypes =
    [
        "ViewCount", "CompletionCount", "CompletionRate",
        "ParticipationTimeline", "TopCategories", "TopIdeas",
        "SurveyDistribution", "IdeaFeed", "ModerationQueue"
    ];

    public async Task<string[]> GetLayoutAsync(string userId, int projectId)
    {
        var prefs = await db.UserWidgetPreferences
            .AsNoTracking()
            .Where(p => p.UserId == userId && p.ProjectId == projectId)
            .OrderBy(p => p.Position)
            .ToListAsync();

        if (prefs.Count == 0)
            return DefaultLayout;

        var layout = new string[9];
        foreach (var p in prefs)
            if (p.Position >= 0 && p.Position < 9)
                layout[p.Position] = p.WidgetType;

        for (var i = 0; i < 9; i++)
            if (string.IsNullOrEmpty(layout[i]))
                layout[i] = DefaultLayout[i];

        return layout;
    }

    public async Task SaveLayoutAsync(string userId, int projectId, string[] widgetTypes)
    {
        var existing = await db.UserWidgetPreferences
            .Where(p => p.UserId == userId && p.ProjectId == projectId)
            .ToListAsync();

        db.UserWidgetPreferences.RemoveRange(existing);

        for (var i = 0; i < Math.Min(widgetTypes.Length, 9); i++)
        {
            db.UserWidgetPreferences.Add(new UserWidgetPreference
            {
                UserId = userId,
                ProjectId = projectId,
                Position = i,
                WidgetType = widgetTypes[i]
            });
        }

        await db.SaveChangesAsync();
    }
}
