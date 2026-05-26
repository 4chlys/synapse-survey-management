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
        string outputLanguage = null,
        int maxOutputChars = 0,
        CancellationToken ct = default)
    {
        var body = FormatIdeas(ideas, isSample: false);
        var userContent = AppendLanguageInstruction(body, outputLanguage);
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, prompt),
            new(ChatRole.User, userContent),
        };
        var result = await SendAsync(messages, client, modelId, projectId, ct);
        return Truncate(result, maxOutputChars);
    }

    public async Task<string> GenerateBatchSummaryAsync(
        IReadOnlyList<Idea> ideas,
        IChatClient client,
        string modelId,
        string prompt,
        int batchSize,
        int projectId,
        string outputLanguage = null,
        int maxOutputChars = 0,
        CancellationToken ct = default)
    {
        var partials = new List<string>();
        int totalBatches = (int)Math.Ceiling((double)ideas.Count / batchSize);

        for (int i = 0; i < ideas.Count; i += batchSize)
        {
            ct.ThrowIfCancellationRequested();
            var chunk = ideas.Skip(i).Take(batchSize).ToList();
            var body = FormatIdeas(chunk, isSample: true);
            var userContent = AppendLanguageInstruction($"[Batch {partials.Count + 1} van {totalBatches}]\n{body}", outputLanguage);
            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, prompt),
                new(ChatRole.User, userContent),
            };
            partials.Add(await SendAsync(messages, client, modelId, projectId, ct));
        }

        var mergeBody = string.Join("\n\n---\n\n", partials.Select((p, i) => $"[Deelsamenvatting {i + 1}]\n{p}"));
        var langInstruction = AppendLanguageInstruction(mergeBody, outputLanguage);
        var mergeMessages = new List<ChatMessage>
        {
            new(ChatRole.System, "Je ontvangt meerdere deelsamenvattingen van ideeën. Combineer ze tot één coherente eindsamenvatting in dezelfde structuur (thema's, meest besproken, controversieel, samenvatting). Vermeld bovenaan dat dit een volledige analyse is van alle ingediende ideeën."),
            new(ChatRole.User, langInstruction),
        };
        var result = await SendAsync(mergeMessages, client, modelId, projectId, ct);
        return Truncate(result, maxOutputChars);
    }

    private static string Truncate(string text, int maxChars)
    {
        if (maxChars <= 0 || text.Length <= maxChars) return text;
        return text[..maxChars] +
               $"\n\n[Summary truncated at {maxChars:N0} characters. Download as TXT for the full text.]";
    }

    private async Task<string> SendAsync(
        IReadOnlyList<ChatMessage> messages,
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

    private static string AppendLanguageInstruction(string body, string outputLanguage)
    {
        if (string.IsNullOrWhiteSpace(outputLanguage)) return body;
        return $"{body}\n\n[Respond in language code: {outputLanguage}]";
    }

    private static string FormatIdeas(IEnumerable<Idea> ideas, bool isSample)
    {
        var lines = new List<string>();
        if (isSample)
            lines.Add("(Dit is een steekproef van alle ingediende ideeën.)");

        lines.AddRange(ideas.Select((idea, i) =>
            $"{i + 1}. [{idea.Comments?.Count ?? 0} reacties] {idea.Description}"));

        return string.Join("\n", lines);
    }
}