using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.BL;

public interface IResponseManager
{
    Task<Response> AddResponseAsync(string value, Question question, Participant participant);
    Task<IEnumerable<Response>> GetAllResponsesAsync();
    Task<IEnumerable<Response>> GetResponsesByProjectIdAsync(int projectId);
}
