using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.BL.Interfaces;

public interface IResponseManager
{
    Task<Response> AddResponseAsync(string value, Question question, Participant participant);
    Task<Response> UpdateOrCreateResponseAsync(string value, Question question, Participant participant);
    Task CommitResponsesAsync(int participantId, HashSet<int> visibleQuestionIds);
    Task<IEnumerable<Response>> GetResponsesForParticipantAsync(int participantId);
}