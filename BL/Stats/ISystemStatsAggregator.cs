using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Stats;

namespace SynapseEnqueteManagement.BL.Stats;

public interface ISystemStatsAggregator
{
    SystemStatsRollup Aggregate(
        IReadOnlyList<Project> projects,
        IReadOnlyList<ProjectParticipantStats> participantStats);
}

public sealed record SystemStatsRollup(
    IReadOnlyList<ProjectStatsRow> ProjectRows,
    IReadOnlyList<PlatformStatsRow> PlatformRows,
    int TotalCompleted,
    double SysAvg,
    double SysMedian);

public sealed record ProjectStatsRow(
    int ProjectId,
    string ProjectName,
    string ShortForm,
    int PlatformId,
    string PlatformName,
    string State,
    int ParticipantCount,
    int StartedCount,
    double AvgCompletionSeconds,
    double MedianCompletionSeconds,
    double MinCompletionSeconds,
    double Q1CompletionSeconds,
    double Q3CompletionSeconds,
    double MaxCompletionSeconds);

public sealed record PlatformStatsRow(
    int PlatformId,
    string Name,
    int ActiveCount,
    int DraftCount,
    int ArchivedCount,
    int ParticipantCount,
    double AvgCompletionSeconds,
    double MedianCompletionSeconds);
