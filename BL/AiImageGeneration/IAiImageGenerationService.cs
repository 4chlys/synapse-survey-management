namespace SynapseEnqueteManagement.BL.AiImageGeneration;

public interface IAiImageGenerationService
{
    /// <summary>
    /// Generates a project header image. Pass a positive <paramref name="projectId"/>
    /// to bill the call against that project; pass 0 when the project does not yet
    /// exist (creation flow) so the call is not attributed.
    /// </summary>
    Task<ImageGenerationResult> GenerateProjectImageAsync(string projectName, int projectId, CancellationToken ct = default);
}
