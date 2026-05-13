namespace SynapseEnqueteManagement.BL.AiSpeechToText;

public interface IAiSpeechToTextService
{
    /// <summary>
    /// Transcribes raw audio bytes to text.
    /// </summary>
    /// <param name="request">Audio data and metadata.</param>
    /// <param name="projectId">Project the call is attributed to for cost tracking.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<SpeechToTextResult> TranscribeAsync(
        SpeechToTextRequest request,
        int projectId,
        CancellationToken ct = default);
}
