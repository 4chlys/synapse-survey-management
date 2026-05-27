using Microsoft.EntityFrameworkCore;
using SynapseEnqueteManagement.UI.Admin.Data;
using SynapseEnqueteManagement.UI.Admin.Models.Dashboard;

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
        "QuestionSinglePie", "QuestionMultiBar", "QuestionLikertBar",
        "SocialMediaPost"
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

    // Build a clean 9-slot array from form input: positions beyond the input fall back to the
    // default layout, unknown widget types are replaced with the slot's default, and QuestionId
    // is only kept for the three question-bound widget types. Pure mapping — kept here next to
    // AllWidgetTypes/DefaultLayout so the rules live in one place.
    public static WidgetSlot[] SanitizeSlots(WidgetSlotInput[] source)
    {
        source ??= [];
        return Enumerable.Range(0, 9)
            .Select(i =>
            {
                var input = i < source.Length ? source[i] : null;
                var type = input is not null && AllWidgetTypes.Contains(input.WidgetType)
                    ? input.WidgetType
                    : DefaultLayout[i];
                var isQuestionBound = type is "QuestionSinglePie" or "QuestionMultiBar" or "QuestionLikertBar";
                return new WidgetSlot(type, isQuestionBound ? input?.QuestionId : null);
            })
            .ToArray();
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

        var maxPos = prefs.Max(p => p.Position);
        var sparse = new WidgetSlot[maxPos + 1];
        foreach (var p in prefs)
            if (p.Position >= 0)
                sparse[p.Position] = new WidgetSlot(p.WidgetType, p.QuestionId);

        var slots = new WidgetSlot[maxPos + 1];
        for (var i = 0; i <= maxPos; i++)
            slots[i] = sparse[i] ?? new WidgetSlot(
                DefaultLayout[Math.Min(i, DefaultLayout.Length - 1)], null);

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

        for (var i = 0; i < slots.Length; i++)
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
