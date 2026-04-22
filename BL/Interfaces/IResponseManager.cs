using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.BL.Interfaces;

public interface IResponseManager
{
    Task<Response> AddResponseAsync(int questionId, string value, Participant participant, CancellationToken ct = default);
    Task<Response> UpdateOrCreateResponseAsync(int questionId, string value, Participant participant, CancellationToken ct = default);
    Task CommitResponsesAsync(int participantId, HashSet<int> visibleQuestionIds, CancellationToken ct = default);
    Task<IEnumerable<Response>> GetResponsesForParticipantAsync(int participantId);
}