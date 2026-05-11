using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.BL.Implementation;

public class ParticipantLanguageCoordinator(
    IParticipantManager participantManager,
    IResponseManager responseManager) : IParticipantLanguageCoordinator
{
    public async Task SwitchLanguageAsync(int participantId, string languageCode, CancellationToken ct = default)
    {
        var newLanguage = await participantManager.SetLanguageAsync(participantId, languageCode, ct);
        await responseManager.RetranslateResponsesForLanguageAsync(participantId, newLanguage, ct);
    }
}
