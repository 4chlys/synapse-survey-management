using Microsoft.EntityFrameworkCore;
using SynapseEnqueteManagement.UI.Admin.Data;

namespace SynapseEnqueteManagement.UI.Admin.Services.Dashboard;

public class WidgetPreferenceService(AdminDbContext db)
{
    public async Task<WidgetSlot[]> GetSlotsAsync(string userId, int projectId)
    {
        var prefs = await db.UserWidgetPreferences
            .AsNoTracking()
            .Where(p => p.UserId == userId && p.ProjectId == projectId)
            .OrderBy(p => p.Position)
            .ToListAsync();

        if (prefs.Count == 0)
            return Array.ConvertAll(WidgetSlotValidator.DefaultLayout, t => new WidgetSlot(t, null));

        // Size to the highest stored position so dashboards aren't truncated to 9 slots.
        var maxPos = prefs.Max(p => p.Position);
        var size = Math.Max(maxPos + 1, WidgetSlotValidator.DefaultLayout.Length);

        var sparse = new WidgetSlot[size];
        foreach (var p in prefs)
            if (p.Position >= 0 && p.Position < size)
                sparse[p.Position] = new WidgetSlot(p.WidgetType, p.QuestionId);

        var slots = new WidgetSlot[size];
        for (var i = 0; i < size; i++)
            slots[i] = sparse[i] ?? new WidgetSlot(
                WidgetSlotValidator.DefaultLayout[Math.Min(i, WidgetSlotValidator.DefaultLayout.Length - 1)], null);

        return slots;
    }

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
