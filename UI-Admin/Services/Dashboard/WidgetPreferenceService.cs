using Microsoft.EntityFrameworkCore;
using SynapseEnqueteManagement.UI.Admin.Data;

namespace SynapseEnqueteManagement.UI.Admin.Services.Dashboard;

public record WidgetSlot(string WidgetType, int? QuestionId);

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
        "SurveyDistribution", "IdeaFeed", "ModerationQueue",
        "QuestionSinglePie", "QuestionMultiBar", "QuestionLikertBar"
    ];

    // Legacy: replaced by GetSlotsAsync/SaveSlotsAsync for question-bound widgets.
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

    // Legacy: replaced by GetSlotsAsync/SaveSlotsAsync for question-bound widgets.
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

    public async Task<WidgetSlot[]> GetSlotsAsync(string userId, int projectId)
    {
        var prefs = await db.UserWidgetPreferences
            .AsNoTracking()
            .Where(p => p.UserId == userId && p.ProjectId == projectId)
            .OrderBy(p => p.Position)
            .ToListAsync();

        if (prefs.Count == 0)
            return Array.ConvertAll(DefaultLayout, t => new WidgetSlot(t, null));

        var sparse = new WidgetSlot[9];
        foreach (var p in prefs)
            if (p.Position >= 0 && p.Position < 9)
                sparse[p.Position] = new WidgetSlot(p.WidgetType, p.QuestionId);

        var slots = new WidgetSlot[9];
        for (var i = 0; i < 9; i++)
            slots[i] = sparse[i] ?? new WidgetSlot(DefaultLayout[i], null);

        return slots;
    }

    // Persists only the question bound to one slot — used by the live question
    // dropdown on the dashboard so the choice survives a reload.
    public async Task UpdateSlotQuestionAsync(string userId, int projectId, int position, int? questionId)
    {
        var slots = await GetSlotsAsync(userId, projectId);
        if (position < 0 || position >= slots.Length)
            return;

        slots[position] = slots[position] with { QuestionId = questionId };
        await SaveSlotsAsync(userId, projectId, slots);
    }

    public async Task SaveSlotsAsync(string userId, int projectId, WidgetSlot[] slots)
    {
        var existing = await db.UserWidgetPreferences
            .Where(p => p.UserId == userId && p.ProjectId == projectId)
            .ToListAsync();

        db.UserWidgetPreferences.RemoveRange(existing);

        for (var i = 0; i < Math.Min(slots.Length, 9); i++)
        {
            db.UserWidgetPreferences.Add(new UserWidgetPreference
            {
                UserId = userId,
                ProjectId = projectId,
                Position = i,
                WidgetType = slots[i].WidgetType,
                QuestionId = slots[i].QuestionId
            });
        }

        await db.SaveChangesAsync();
    }
}
