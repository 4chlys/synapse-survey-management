using ScottPlot;
using SynapseEnqueteManagement.API.DTOs.Stats;

namespace SynapseEnqueteManagement.API.Services;

public class ScottPlotChartImageService : IChartImageService
{
    private const int Width = 900;
    private const int Height = 540;
    private const int TopProjectsForBar = 20;

    public IReadOnlyList<(string Stem, byte[] PngBytes)> RenderAll(SystemStatsDto stats) =>
        new List<(string, byte[])>
        {
            ("participants-per-project", RenderParticipantsBar(stats)),
            ("project-states",           RenderProjectStatesDoughnut(stats)),
            ("ai-cost-per-service",      RenderAiCostBar(stats)),
            ("completion-time-boxplot",  RenderCompletionBoxPlot(stats)),
        };

    public byte[] RenderCompletionBoxPlot(SystemStatsDto stats)
    {
        var projects = stats.Projects
            .Where(p => p.ParticipantCount >= 1)
            .OrderBy(p => p.ProjectName)
            .ToList();

        var plot = new Plot();
        plot.Title("Completion time per project (seconds)");

        if (projects.Count == 0)
        {
            plot.Axes.Bottom.Label.Text = "No completed participants";
            return plot.GetImageBytes(Width, Height, ImageFormat.Png);
        }

        var boxes = new List<ScottPlot.Box>();
        for (int i = 0; i < projects.Count; i++)
        {
            var p = projects[i];
            boxes.Add(new ScottPlot.Box
            {
                Position = i + 1,
                BoxMin = p.Q1CompletionSeconds,
                BoxMax = p.Q3CompletionSeconds,
                BoxMiddle = p.MedianCompletionSeconds,
                WhiskerMin = p.MinCompletionSeconds,
                WhiskerMax = p.MaxCompletionSeconds,
            });
        }

        plot.Add.Boxes(boxes);
        plot.Axes.Bottom.SetTicks(
            Enumerable.Range(1, projects.Count).Select(i => (double)i).ToArray(),
            projects.Select(p => Truncate(p.ProjectName, 14)).ToArray());
        plot.Axes.Bottom.TickLabelStyle.Rotation = -30;
        plot.Axes.Left.Label.Text = "Seconds";

        return plot.GetImageBytes(Width, Height, ImageFormat.Png);
    }

    private static byte[] RenderParticipantsBar(SystemStatsDto stats)
    {
        var top = stats.Projects
            .OrderByDescending(p => p.ParticipantCount)
            .Take(TopProjectsForBar)
            .ToList();

        var plot = new Plot();
        plot.Title($"Participants per project (top {top.Count})");

        if (top.Count == 0)
            return plot.GetImageBytes(Width, Height, ImageFormat.Png);

        var bars = top.Select((p, i) => new Bar
        {
            Position = i + 1,
            Value = p.ParticipantCount,
            Label = p.ParticipantCount.ToString(),
        }).ToList();
        plot.Add.Bars(bars);

        plot.Axes.Bottom.SetTicks(
            Enumerable.Range(1, top.Count).Select(i => (double)i).ToArray(),
            top.Select(p => Truncate(p.ProjectName, 14)).ToArray());
        plot.Axes.Bottom.TickLabelStyle.Rotation = -30;
        plot.Axes.Left.Label.Text = "Participants";

        return plot.GetImageBytes(Width, Height, ImageFormat.Png);
    }

    private static byte[] RenderProjectStatesDoughnut(SystemStatsDto stats)
    {
        var plot = new Plot();
        plot.Title("Project states");

        var slices = new List<PieSlice>
        {
            new() { Value = stats.ActiveProjectCount,   Label = $"Active ({stats.ActiveProjectCount})",     FillColor = Colors.Green },
            new() { Value = stats.DraftProjectCount,    Label = $"Draft ({stats.DraftProjectCount})",       FillColor = Colors.Orange },
            new() { Value = stats.ArchivedProjectCount, Label = $"Archived ({stats.ArchivedProjectCount})", FillColor = Colors.Gray },
        };

        if (slices.Sum(s => s.Value) <= 0)
            return plot.GetImageBytes(Width, Height, ImageFormat.Png);

        var pie = plot.Add.Pie(slices);
        pie.DonutFraction = 0.5;
        pie.SliceLabelDistance = 1.2;
        plot.Axes.Frameless();
        plot.HideGrid();

        return plot.GetImageBytes(Width, Height, ImageFormat.Png);
    }

    private static byte[] RenderAiCostBar(SystemStatsDto stats)
    {
        var byService = stats.AiUsageBySystem
            .GroupBy(s => s.Service)
            .Select(g => (Service: g.Key, Cost: g.Sum(x => (double)x.CostEur)))
            .OrderByDescending(x => x.Cost)
            .ToList();

        var plot = new Plot();
        plot.Title("AI cost (EUR) per service");

        if (byService.Count == 0)
            return plot.GetImageBytes(Width, Height, ImageFormat.Png);

        var bars = byService.Select((s, i) => new Bar
        {
            Position = i + 1,
            Value = s.Cost,
            Label = s.Cost.ToString("F4"),
        }).ToList();
        plot.Add.Bars(bars);

        plot.Axes.Bottom.SetTicks(
            Enumerable.Range(1, byService.Count).Select(i => (double)i).ToArray(),
            byService.Select(s => s.Service).ToArray());
        plot.Axes.Bottom.TickLabelStyle.Rotation = -30;
        plot.Axes.Left.Label.Text = "EUR";

        return plot.GetImageBytes(Width, Height, ImageFormat.Png);
    }

    public IReadOnlyList<(string Stem, byte[] PngBytes)> RenderProjectCharts(ProjectExportDataDto data) =>
        [
            ("participation-timeline", RenderTimeline(data)),
            ("top-categories",         RenderTopCategories(data)),
        ];

    private static byte[] RenderTimeline(ProjectExportDataDto data)
    {
        var plot = new Plot();
        plot.Title("Participation (30 days)");

        if (data.TimelineStarts.Count == 0)
            return plot.GetImageBytes(Width, Height, ImageFormat.Png);

        var xs = Enumerable.Range(0, data.TimelineDates.Count).Select(i => (double)i).ToArray();
        var starts = data.TimelineStarts.Select(v => (double)v).ToArray();
        var completions = data.TimelineCompletions.Select(v => (double)v).ToArray();

        var startScatter = plot.Add.Scatter(xs, starts);
        startScatter.LegendText = "Started";
        var completionScatter = plot.Add.Scatter(xs, completions);
        completionScatter.LegendText = "Completed";

        var step = Math.Max(1, xs.Length / 8);
        var tickPositions = xs.Where((_, i) => i % step == 0).ToArray();
        var tickLabels = data.TimelineDates
            .Where((_, i) => i % step == 0)
            .Select(d => d.Length >= 10 ? d[5..] : d)
            .ToArray();
        plot.Axes.Bottom.SetTicks(tickPositions, tickLabels);
        plot.Axes.Bottom.TickLabelStyle.Rotation = -30;
        plot.Axes.Left.Label.Text = "Count";
        plot.Legend.IsVisible = true;

        return plot.GetImageBytes(Width, Height, ImageFormat.Png);
    }

    private static byte[] RenderTopCategories(ProjectExportDataDto data)
    {
        var plot = new Plot();
        plot.Title("Top categories");

        if (data.TopCategories.Count == 0)
            return plot.GetImageBytes(Width, Height, ImageFormat.Png);

        var ideaBars = data.TopCategories.Select((c, i) => new Bar
        {
            Position = i * 3 + 1,
            Value = c.IdeaCount,
            Label = c.IdeaCount.ToString(),
        }).ToList();
        var reactionBars = data.TopCategories.Select((c, i) => new Bar
        {
            Position = i * 3 + 2,
            Value = c.TotalReactions,
            Label = c.TotalReactions.ToString(),
        }).ToList();

        plot.Add.Bars(ideaBars);
        plot.Add.Bars(reactionBars);

        var labelPositions = data.TopCategories.Select((_, i) => (double)(i * 3 + 1.5)).ToArray();
        plot.Axes.Bottom.SetTicks(labelPositions, data.TopCategories.Select(c => Truncate(c.Name, 14)).ToArray());
        plot.Axes.Bottom.TickLabelStyle.Rotation = -30;
        plot.Axes.Left.Label.Text = "Count";

        return plot.GetImageBytes(Width, Height, ImageFormat.Png);
    }

    private static string Truncate(string s, int max) =>
        string.IsNullOrEmpty(s) ? string.Empty : (s.Length <= max ? s : s[..max] + "…");
}
