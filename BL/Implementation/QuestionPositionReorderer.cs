using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.BL.Implementation;

internal static class QuestionPositionReorderer
{
    public static void Reorder(
        List<Question> siblings, int? excludeId, int? insertId, Question insertTarget, int? insertAt)
    {
        if (excludeId.HasValue)
            siblings.RemoveAll(s => s.QuestionId == excludeId.Value);

        if (insertId.HasValue)
        {
            siblings.RemoveAll(s => s.QuestionId == insertId.Value);
            if (insertTarget is not null)
            {
                var clamped = Math.Clamp(insertAt ?? siblings.Count, 0, siblings.Count);
                siblings.Insert(clamped, insertTarget);
            }
        }

        for (var i = 0; i < siblings.Count; i++)
        {
            var desired = i + 1;
            if (siblings[i].Position != desired)
                siblings[i].Position = desired;
        }
    }
}
