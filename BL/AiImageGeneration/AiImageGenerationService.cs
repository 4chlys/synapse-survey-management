using System.Net;
using System.Text.RegularExpressions;
using Google.Cloud.AIPlatform.V1;
using Microsoft.Extensions.Logging;

namespace SynapseEnqueteManagement.BL.AiImageGeneration;

public sealed class AiImageGenerationService(
    PredictionServiceClient predictionClient,
    string modelResource,
    AiImageGenerationConfig config,
    ILogger<AiImageGenerationService> logger) : IAiImageGenerationService
{
    // Strip HTML tags, decode entities (&amp; → &, &lt; → <, etc.), and collapse whitespace
    // for use in a text prompt. Quill emits entities for special characters, so decoding
    // must happen after tag removal to avoid re-introducing markup.
    private static string StripHtml(string html) =>
        WebUtility.HtmlDecode(Regex.Replace(Regex.Replace(html, "<[^>]+>", " "), @"\s{2,}", " ").Trim());

    public async Task<ImageGenerationResult> GenerateProjectImageAsync(
        string projectName,
        int projectId,
        string projectTopic = null,
        string introText = null,
        CancellationToken ct = default)
    {
        try
        {
            // Build optional context block: topic (highest priority), then intro snippet.
            // Phrased as scene guidance so the model generates concrete real-world imagery.
            var contextParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(projectTopic))
                contextParts.Add($" Het gaat specifiek over: \"{projectTopic.Trim()}\".");
            if (!string.IsNullOrWhiteSpace(introText))
            {
                var stripped = StripHtml(introText);
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
