namespace SynapseEnqueteManagement.BL.Domain.Stats;

public sealed record ProjectParticipantStats(
    int ProjectId,
    int StartedCount,
    int CompletedCount,
    double AvgCompletionSeconds,
    double MedianCompletionSeconds,
    double MinCompletionSeconds,
    double Q1CompletionSeconds,
    double Q3CompletionSeconds,
    double MaxCompletionSeconds,
    IReadOnlyList<double> SortedDurationsSeconds);
