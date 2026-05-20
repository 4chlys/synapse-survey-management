using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using SynapseEnqueteManagement.BL.Ai;
using SynapseEnqueteManagement.BL.AiUsage;
using SynapseEnqueteManagement.BL.Domain.Ai.Entities;

namespace SynapseEnqueteManagement.BL.AiImageDescription;

public sealed class AiImageDescriptionService(
    AiImageDescriptionConfig config,
    IAiUsageManager usageManager,
    ILogger<AiImageDescriptionService> logger) : IAiImageDescriptionService
{
    public async Task<string> DescribeImageAsync(byte[] imageBytes, string mimeType, string language,
        string categoryName, string categoryDescription,
        ResolvedPlatformAiConfig platformConfig, int projectId, CancellationToken ct = default)
    {
        try
        {
            var prompt = config.SystemPrompt
                .Replace("{category_name}", categoryName, StringComparison.OrdinalIgnoreCase)
                .Replace("{category_description}", categoryDescription, StringComparison.OrdinalIgnoreCase)
                .Replace("{language}", language, StringComparison.OrdinalIgnoreCase);

            var imageContent = new DataContent(new ReadOnlyMemory<byte>(imageBytes), mimeType);
            var message = new ChatMessage(ChatRole.User,
            [
                imageContent,
                new TextContent(prompt),
            ]);

            var response = await platformConfig.ChatClient.GetResponseAsync([message], cancellationToken: ct);
            try { await usageManager.LogChatAsync(projectId, AiServiceKind.ImageDescription, platformConfig.SelectedModelId, response.Usage, ct); }
            catch (Exception ex) { logger.LogWarning(ex, "[ImageDescription] Failed to record AI usage."); }

            var description = response.Messages.LastOrDefault()?.Text?.Trim();

            return string.IsNullOrWhiteSpace(description) ? string.Empty : description;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[ImageDescription] AI call failed.");
            return string.Empty;
        }
    }
}