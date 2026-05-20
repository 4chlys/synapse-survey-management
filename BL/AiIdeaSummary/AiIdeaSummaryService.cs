using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using SynapseEnqueteManagement.BL.AiUsage;
using SynapseEnqueteManagement.BL.Domain.Ai.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.BL.AiIdeaSummary;

public sealed class AiIdeaSummaryService(
    ILogger<AiIdeaSummaryService> logger,
    IAiUsageManager usageManager) : IAiIdeaSummaryService
{
    public async Task<string> GenerateSummaryAsync(
        IReadOnlyList<Idea> ideas,
        IChatClient client,
        string modelId,
        string prompt,
        int projectId,
        CancellationToken ct = default)
    {
        var body = FormatIdeas(ideas, isSample: false);
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, prompt),
            new(ChatRole.User, body),
        };
        return await SendAsync(messages, client, modelId, projectId, ct);
    }

    public async Task<string> GenerateBatchSummaryAsync(
        IReadOnlyList<Idea> ideas,
        IChatClient client,
        string modelId,
        string prompt,
        int batchSize,
        int projectId,
        CancellationToken ct = default)
    {
        var partials = new List<string>();
        int totalBatches = (int)Math.Ceiling((double)ideas.Count / batchSize);

        for (int i = 0; i < ideas.Count; i += batchSize)
        {
            ct.ThrowIfCancellationRequested();
            var chunk = ideas.Skip(i).Take(batchSize).ToList();
            var body = FormatIdeas(chunk, isSample: true);
            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, prompt),
                new(ChatRole.User, $"[Batch {partials.Count + 1} van {totalBatches}]\n{body}"),
            };
            partials.Add(await SendAsync(messages, client, modelId, projectId, ct));
        }

        var mergeBody = string.Join("\n\n---\n\n", partials.Select((p, i) => $"[Deelsamenvatting {i + 1}]\n{p}"));
        var mergeMessages = new List<ChatMessage>
        {
            new(ChatRole.System, "Je ontvangt meerdere deelsamenvattingen van ideeën. Combineer ze tot één coherente eindsamenvatting in dezelfde structuur (thema's, meest besproken, controversieel, samenvatting). Vermeld bovenaan dat dit een volledige analyse is van alle ingediende ideeën."),
            new(ChatRole.User, mergeBody),
        };
        return await SendAsync(mergeMessages, client, modelId, projectId, ct);
    }

    private async Task<string> SendAsync(
        List<ChatMessage> messages,
        IChatClient client,
        string modelId,
        int projectId,
        CancellationToken ct)
    {
        var response = await client.GetResponseAsync(messages, cancellationToken: ct);
        try
        {
            await usageManager.LogChatAsync(projectId, AiServiceKind.IdeaSummary, modelId, response.Usage, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[IdeaSummary] Failed to record AI usage.");
        }
        return response.Messages.LastOrDefault()?.Text?.Trim() ?? string.Empty;
    }

    private static string FormatIdeas(IEnumerable<Idea> ideas, bool isSample)
    {
        var lines = new List<string>();
        if (isSample)
            lines.Add("(Dit is een steekproef van alle ingediende ideeën.)");

        int n = 1;
        foreach (var idea in ideas)
        {
            int commentCount = idea.Comments?.Count ?? 0;
            lines.Add($"{n}. [{commentCount} reacties] {idea.Description}");
            n++;
        }
        return string.Join("\n", lines);
    }
}