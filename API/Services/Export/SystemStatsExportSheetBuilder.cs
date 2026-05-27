using SynapseEnqueteManagement.API.DTOs.Stats;

namespace SynapseEnqueteManagement.API.Services.Export;

public static class SystemStatsExportSheetBuilder
{
    public static readonly IReadOnlySet<string> AllSections =
        new HashSet<string> { "platforms", "projects", "summary", "ai-system", "ai-platforms", "ai-projects" };

    public static IReadOnlyList<ExportSheet> Build(SystemStatsDto stats, IReadOnlySet<string> sections)
    {
        var sheets = new List<ExportSheet>();

        if (sections.Contains("platforms"))
            sheets.Add(new ExportSheet("platforms", "Platforms",
                ["Platform", "Active", "Draft", "Archived", "Participants", "AvgTime", "MedianTime"],
                stats.Platforms.Select(r => new[]
                {
                    r.Name,
                    r.ActiveCount.ToString(), r.DraftCount.ToString(), r.ArchivedCount.ToString(),
                    r.ParticipantCount.ToString(),
                    r.AvgCompletionSeconds.ToString("F1"), r.MedianCompletionSeconds.ToString("F1"),
                })));

        if (sections.Contains("projects"))
            sheets.Add(new ExportSheet("projects", "Projects",
                ["Platform", "Project", "Short", "State", "Participants", "Started", "AvgTime", "MedianTime"],
                stats.Projects.Select(r => new[]
                {
                    r.PlatformName, r.ProjectName, r.ShortForm, r.State,
                    r.ParticipantCount.ToString(), r.StartedCount.ToString(),
                    r.AvgCompletionSeconds.ToString("F1"), r.MedianCompletionSeconds.ToString("F1"),
                })));

        if (sections.Contains("summary"))
            sheets.Add(new ExportSheet("summary", "Summary", ["Metric", "Value"],
            [
                ["Platforms", stats.PlatformCount.ToString()],
                ["Active projects", stats.ActiveProjectCount.ToString()],
                ["Draft projects", stats.DraftProjectCount.ToString()],
                ["Archived projects", stats.ArchivedProjectCount.ToString()],
                ["Total participants", stats.TotalParticipants.ToString()],
                ["Avg completion (s)", stats.AvgCompletionSeconds.ToString("F1")],
                ["Median completion (s)", stats.MedianCompletionSeconds.ToString("F1")],
            ]));

        if (sections.Contains("ai-system"))
            sheets.Add(new ExportSheet("ai-system", "AI by service",
                ["Service", "Model", "InputTokens", "OutputTokens", "Requests", "Cost (EUR)"],
                stats.AiUsageBySystem.Select(s => new[]
                {
                    s.Service, s.Model,
                    s.InputTokens.ToString(), s.OutputTokens.ToString(),
                    s.RequestCount.ToString(), s.CostEur.ToString("F4"),
                })));

        if (sections.Contains("ai-platforms"))
            sheets.Add(new ExportSheet("ai-platforms", "AI by platform",
                ["Platform", "Service", "Model", "InputTokens", "OutputTokens", "Requests", "Cost (EUR)"],
                stats.AiUsageByPlatform.Select(s => new[]
                {
                    s.ProjectPlatformName, s.Service, s.Model,
                    s.InputTokens.ToString(), s.OutputTokens.ToString(),
                    s.RequestCount.ToString(), s.CostEur.ToString("F4"),
                })));

        if (sections.Contains("ai-projects"))
            sheets.Add(new ExportSheet("ai-projects", "AI by project",
                ["Platform", "Project", "Service", "Model", "InputTokens", "OutputTokens", "Requests", "Cost (EUR)"],
                stats.AiUsageByProject.Select(s => new[]
                {
                    s.ProjectPlatformName, s.ShortForm, s.Service, s.Model,
                    s.InputTokens.ToString(), s.OutputTokens.ToString(),
                    s.RequestCount.ToString(), s.CostEur.ToString("F4"),
                })));

        return sheets;
    }
}
