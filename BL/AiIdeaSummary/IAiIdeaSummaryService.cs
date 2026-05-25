using Microsoft.Extensions.AI;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.BL.AiIdeaSummary;

public interface IAiIdeaSummaryService
{
    Task<string> GenerateSummaryAsync(
        IReadOnlyList<Idea> ideas,
        IChatClient client,
        string modelId,
        string prompt,
        int projectId,
        string outputLanguage = null,
        CancellationToken ct = default);

    Task<string> GenerateBatchSummaryAsync(
        IReadOnlyList<Idea> ideas,
        IChatClient client,
        string modelId,
        string prompt,
        int batchSize,
        int projectId,
        string outputLanguage = null,
        CancellationToken ct = default);
}