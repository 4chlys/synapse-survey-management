using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.BL.Domain.Stats;
using SynapseEnqueteManagement.BL.Helpers;

namespace SynapseEnqueteManagement.BL.Stats;

public sealed class SystemStatsAggregator : ISystemStatsAggregator
{
    public SystemStatsRollup Aggregate(
        IReadOnlyList<Project> projects,
        IReadOnlyList<ProjectParticipantStats> participantStats)
    {
        var statsById = participantStats.ToDictionary(s => s.ProjectId);

        var projectRows = projects.Select(p =>
        {
            statsById.TryGetValue(p.ProjectId, out var ps);
            var name = TranslationPicker.Pick(p.Translations, p.DefaultLanguage?.Code)?.Name
                       ?? string.Empty;
            return new ProjectStatsRow(
                ProjectId:               p.ProjectId,
                ProjectName:             name,
                ShortForm:               p.ShortForm,
                PlatformId:              p.ProjectPlatformId,
                PlatformName:            p.ProjectPlatform?.Name ?? string.Empty,
                State:                   p.ProjectState.ToString(),
                ParticipantCount:        ps?.CompletedCount ?? 0,
                StartedCount:            ps?.StartedCount ?? 0,
                AvgCompletionSeconds:    ps?.AvgCompletionSeconds ?? 0,
                MedianCompletionSeconds: ps?.MedianCompletionSeconds ?? 0,
                MinCompletionSeconds:    ps?.MinCompletionSeconds ?? 0,
                Q1CompletionSeconds:     ps?.Q1CompletionSeconds ?? 0,
                Q3CompletionSeconds:     ps?.Q3CompletionSeconds ?? 0,
                MaxCompletionSeconds:    ps?.MaxCompletionSeconds ?? 0);
        }).ToList();

        var platformRows = projects
            .GroupBy(p => p.ProjectPlatformId)
            .Select(g =>
            {
                var gProjects = g.ToList();
                var gStats = gProjects
                    .Select(p => statsById.GetValueOrDefault(p.ProjectId))
                    .Where(s => s is not null)
                    .ToList();
                int gCompleted = gStats.Sum(s => s.CompletedCount);
                double gAvg = gCompleted > 0
                    ? gStats.Sum(s => s.AvgCompletionSeconds * s.CompletedCount) / gCompleted
                    : 0;
                double gMedian = gStats.Count > 0 ? gStats.Average(s => s.MedianCompletionSeconds) : 0;
                return new PlatformStatsRow(
                    PlatformId:              g.Key,
                    Name:                    gProjects.First().ProjectPlatform?.Name ?? string.Empty,
                    ActiveCount:             gProjects.Count(p => p.ProjectState == ProjectState.Active),
                    DraftCount:              gProjects.Count(p => p.ProjectState == ProjectState.Draft),
                    ArchivedCount:           gProjects.Count(p => p.ProjectState == ProjectState.Archived),
                    ParticipantCount:        gCompleted,
                    AvgCompletionSeconds:    gAvg,
                    MedianCompletionSeconds: gMedian);
            }).ToList();

        int totalCompleted = participantStats.Sum(s => s.CompletedCount);
        double sysAvg = totalCompleted > 0
            ? participantStats.Sum(s => s.AvgCompletionSeconds * s.CompletedCount) / totalCompleted
            : 0;
        double sysMedian = participantStats.Count > 0
            ? participantStats.Average(s => s.MedianCompletionSeconds)
            : 0;

        return new SystemStatsRollup(projectRows, platformRows, totalCompleted, sysAvg, sysMedian);
    }
}
