using SynapseEnqueteManagement.API.DTOs.Stats;

namespace SynapseEnqueteManagement.API.Services.Export;

public static class ProjectStatsExportSheetBuilder
{
    public static readonly IReadOnlySet<string> AllSections =
        new HashSet<string> { "summary", "timeline", "categories", "ideas" };

    public static IReadOnlyList<ExportSheet> Build(ProjectExportDataDto data, IReadOnlySet<string> sections)
    {
        var sheets = new List<ExportSheet>();

        if (sections.Contains("summary"))
            sheets.Add(new ExportSheet("summary", "Summary", ["Metric", "Value"],
            [
                ["Views", data.ViewCount.ToString()],
                ["Completions", data.CompletionCount.ToString()],
                ["Completion rate", (data.CompletionRate * 100).ToString("F1") + "%"],
            ]));

        if (sections.Contains("timeline"))
            sheets.Add(new ExportSheet("timeline", "Timeline",
                ["Date", "Started", "Completed"],
                data.TimelineDates.Select((d, i) => new[]
                {
                    d,
                    data.TimelineStarts.ElementAtOrDefault(i).ToString(),
                    data.TimelineCompletions.ElementAtOrDefault(i).ToString(),
                })));

        if (sections.Contains("categories"))
            sheets.Add(new ExportSheet("categories", "Categories",
                ["Category", "Ideas", "Reactions"],
                data.TopCategories.Select(c => new[]
                {
                    c.Name,
                    c.IdeaCount.ToString(),
                    c.TotalReactions.ToString(),
                })));

        if (sections.Contains("ideas"))
            sheets.Add(new ExportSheet("ideas", "Ideas",
                ["Description", "Reactions", "Comments"],
                data.TopIdeas.Select(i => new[]
                {
                    i.Description ?? string.Empty,
                    i.ReactionCount.ToString(),
                    i.CommentCount.ToString(),
                })));

        return sheets;
    }
}
