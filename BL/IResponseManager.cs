using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.BL;

public interface IResponseManager
{
    Task<Response> AddResponseAsync(string value, Question question, Participant participant);
    Task AddResponsesAsync(Participant participant, IEnumerable<(int QuestionId, string Value)> answers);
    Task<IEnumerable<Response>> GetResponsesByProjectIdAsync(int projectId);
}