using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.BL.Interfaces;

public interface IResponseManager
{
    Task<Response> AddResponseAsync(string value, Question question, Participant participant, CancellationToken ct = default);
    Task<Response> UpdateOrCreateResponseAsync(string value, Question question, Participant participant, CancellationToken ct = default);
    Task CommitResponsesAsync(int participantId, HashSet<int> visibleQuestionIds, CancellationToken ct = default);
    Task<IEnumerable<Response>> GetResponsesForParticipantAsync(int participantId);
}