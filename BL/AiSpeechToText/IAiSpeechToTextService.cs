using SynapseEnqueteManagement.BL.Ai;

namespace SynapseEnqueteManagement.BL.AiSpeechToText;

public interface IAiSpeechToTextService
{
    Task<SpeechToTextResult> TranscribeAsync(
        SpeechToTextRequest request,
        ResolvedPlatformAiConfig platformConfig,
        CancellationToken ct = default);
}
