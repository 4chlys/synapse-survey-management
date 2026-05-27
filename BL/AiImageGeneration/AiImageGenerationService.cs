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
            if (!string.IsNullOrWhiteSpace(projectTopic))
                contextParts.Add($" Het gaat specifiek over: \"{projectTopic.Trim()}\".");
            if (!string.IsNullOrWhiteSpace(introText))
            {
                var stripped = HtmlHelpers.StripHtml(introText);
                if (stripped.Length > 300)
                {
                    var end = 300;
                    if (char.IsHighSurrogate(stripped[end - 1])) end--;
                    stripped = stripped[..end].TrimEnd() + "…";
                }
                if (!string.IsNullOrWhiteSpace(stripped))
                    contextParts.Add($" Achtergrondinformatie: \"{stripped}\".");
            }
            var context = string.Concat(contextParts);

            string prompt;
            if (!string.IsNullOrWhiteSpace(postCopy))
            {
                string trimmed;
                var limit = 500;
                if (postCopy.Length > limit)
                {
                    if (char.IsHighSurrogate(postCopy[limit - 1])) limit--;
                    trimmed = postCopy[..limit].TrimEnd() + "…";
                }
                else
                    trimmed = postCopy;
                prompt = config.SocialPrompt
                    .Replace("{post_copy}", trimmed)
                    .Replace("{project_name}", projectName)
                    .Replace("{context}", context);
            }
            else
            {
                prompt = config.Prompt
                    .Replace("{project_name}", projectName)
                    .Replace("{context}", context);
            }

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
