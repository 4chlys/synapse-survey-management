using Google.Cloud.AIPlatform.V1;
using Microsoft.Extensions.Logging;
using Value = Google.Protobuf.WellKnownTypes.Value;
using Struct = Google.Protobuf.WellKnownTypes.Struct;

namespace SynapseEnqueteManagement.BL.AiImageGeneration;

public sealed class GoogleVertexAiImageGenerationService(
    PredictionServiceClient predictionClient,
    string modelResource,
    AiImageGenerationConfig config,
    ILogger<GoogleVertexAiImageGenerationService> logger) : IAiImageGenerationService
{
    public async Task<ImageGenerationResult> GenerateProjectImageAsync(string projectName, CancellationToken ct = default)
    {
        try
        {
            var prompt = config.Prompt.Replace("{project_name}", projectName);

            var instance = Value.ForStruct(new Struct
            {
                Fields = { ["prompt"] = Value.ForString(prompt) }
            });

            var parameters = Value.ForStruct(new Struct
            {
                Fields =
                {
                    ["aspectRatio"] = Value.ForString(config.AspectRatio),
                }
            });

            var response = await predictionClient.PredictAsync(
                modelResource, [instance], parameters, ct);

            if (response.Predictions is null || response.Predictions.Count == 0)
            {
                logger.LogWarning("[ImageGeneration] Imagen returned no predictions for project '{Name}'", projectName);
                return ImageGenerationResult.Failed("Afbeelding kon niet worden gegenereerd.");
            }

            var prediction = response.Predictions[0].StructValue;
            var base64 = prediction.Fields["bytesBase64Encoded"].StringValue;
            var mimeType = prediction.Fields.TryGetValue("mimeType", out var mt)
                ? mt.StringValue
                : "image/png";

            return ImageGenerationResult.Success(Convert.FromBase64String(base64), mimeType);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[ImageGeneration] Failed to generate image for project '{Name}'", projectName);
            return ImageGenerationResult.Failed("Afbeelding kon niet worden gegenereerd.");
        }
    }
}
