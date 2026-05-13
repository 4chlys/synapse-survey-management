using Google.Cloud.AIPlatform.V1;
using Microsoft.Extensions.Logging;
using SynapseEnqueteManagement.BL.AiUsage;

namespace SynapseEnqueteManagement.BL.AiImageGeneration;

public sealed class GoogleVertexAiImageGenerationService(
    PredictionServiceClient predictionClient,
    string modelResource,
    AiImageGenerationConfig config,
    IAiUsageManager usageManager,
    ILogger<GoogleVertexAiImageGenerationService> logger) : IAiImageGenerationService
{
    public async Task<ImageGenerationResult> GenerateProjectImageAsync(string projectName, int projectId, CancellationToken ct = default)
    {
        try
        {
            var prompt = config.Prompt.Replace("{project_name}", projectName);

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
                    try
                    {
                        await usageManager.LogImageAsync(projectId, config.Model, 1, config.Width, config.Height, ct);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "[ImageGeneration] Failed to record AI usage.");
                    }

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
