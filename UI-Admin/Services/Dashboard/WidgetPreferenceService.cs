using Microsoft.EntityFrameworkCore;
using SynapseEnqueteManagement.UI.Admin.Data;
using SynapseEnqueteManagement.UI.Admin.Models.Dashboard;

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

        var sparse = new WidgetSlot[9];
        foreach (var p in prefs)
            if (p.Position >= 0 && p.Position < 9)
                sparse[p.Position] = new WidgetSlot(p.WidgetType, p.QuestionId);

        var slots = new WidgetSlot[9];
        for (var i = 0; i < 9; i++)
            slots[i] = sparse[i] ?? new WidgetSlot(WidgetSlotValidator.DefaultLayout[i], null);

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
