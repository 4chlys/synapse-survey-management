namespace SynapseEnqueteManagement.BL.AiImageGeneration;

public interface IAiImageGenerationService
{
    /// <summary>
    /// Generates a project header image. Pass a positive <paramref name="projectId"/>
    /// to bill the call against that project; pass 0 when the project does not yet
    /// exist (creation flow) so the call is not attributed.
    /// <paramref name="projectTopic"/> and <paramref name="introText"/> are optional context
    /// that improve prompt quality; <paramref name="introText"/> may contain HTML and will be stripped.
    /// </summary>
    Task<ImageGenerationResult> GenerateProjectImageAsync(
        string projectName,
        int projectId,
        string projectTopic = null,
        string introText = null,
        string postCopy = null,
        CancellationToken ct = default);
}
