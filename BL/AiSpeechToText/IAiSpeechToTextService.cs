namespace SynapseEnqueteManagement.BL.AiSpeechToText;

public interface IAiSpeechToTextService
{
    /// <summary>
    /// Transcribes raw audio bytes to text.
    /// </summary>
    /// <param name="request">Audio data and metadata.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<SpeechToTextResult> TranscribeAsync(
        SpeechToTextRequest request,
        CancellationToken ct = default);
}
