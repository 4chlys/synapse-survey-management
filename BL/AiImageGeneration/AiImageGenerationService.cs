using Google.Cloud.AIPlatform.V1;
using Microsoft.Extensions.Logging;
using SynapseEnqueteManagement.BL.Helpers;

namespace SynapseEnqueteManagement.BL.AiImageGeneration;

public sealed class AiImageGenerationService(
    PredictionServiceClient predictionClient,
    string modelResource,
    AiImageGenerationConfig config,
    ILogger<AiImageGenerationService> logger) : IAiImageGenerationService
{
    public async Task<ImageGenerationResult> GenerateProjectImageAsync(
        string projectName,
        int projectId,
        string projectTopic = null,
        string introText = null,
        string postCopy = null,
        CancellationToken ct = default)
    {
        try
        {
            var contextParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(postCopy))
            {
                var trimmed = postCopy.Length > 500 ? postCopy[..500].TrimEnd() + "…" : postCopy;
                contextParts.Add($" De bijbehorende social media post: \"{trimmed}\".");
            }
            if (!string.IsNullOrWhiteSpace(projectTopic))
                contextParts.Add($" Het gaat specifiek over: \"{projectTopic.Trim()}\".");
            if (!string.IsNullOrWhiteSpace(introText))
            {
                var stripped = HtmlHelpers.StripHtml(introText);
                if (stripped.Length > 300)
                    stripped = stripped[..300].TrimEnd() + "…";
                if (!string.IsNullOrWhiteSpace(stripped))
                    contextParts.Add($" Achtergrondinformatie: \"{stripped}\".");
            }
            var context = string.Concat(contextParts);

            var prompt = config.Prompt
                .Replace("{project_name}", projectName)
                .Replace("{context}", context);

            var request = new GenerateContentRequest
            {
                Model = modelResource,
                Contents =
                {
                    new Content
                    {
                        Role = "USER",
                        Parts = { new Part { Text = prompt } }
                    }
                },
                GenerationConfig = new GenerationConfig
                {
                    ResponseModalities =
                    {
                        GenerationConfig.Types.Modality.Image,
                        GenerationConfig.Types.Modality.Text
                    }
                }
            };

            var response = await predictionClient.GenerateContentAsync(request, ct);

            if (response.Candidates is null || response.Candidates.Count == 0)
            {
                logger.LogWarning("[ImageGeneration] Gemini returned no candidates for project '{Name}'", projectName);
                return ImageGenerationResult.Failed("Image could not be generated.");
            }

            foreach (var part in response.Candidates[0].Content.Parts)
            {
                if (part.InlineData is { } inlineData && inlineData.MimeType.StartsWith("image/"))
                {
                    return ImageGenerationResult.Success(
                        inlineData.Data.ToByteArray(),
                        inlineData.MimeType);
                }
            }

            logger.LogWarning("[ImageGeneration] Gemini response contained no image data for project '{Name}'", projectName);
            return ImageGenerationResult.Failed("Image could not be generated.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[ImageGeneration] Failed to generate image for project '{Name}'", projectName);
            return ImageGenerationResult.Failed("Image could not be generated.");
        }
    }
}
