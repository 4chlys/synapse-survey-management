namespace SynapseEnqueteManagement.BL.Interfaces;

public interface IParticipantLanguageCoordinator
{
    Task SwitchLanguageAsync(int participantId, string languageCode, CancellationToken ct = default);
}
