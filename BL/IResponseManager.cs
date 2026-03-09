using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.BL;

public interface IResponseManager
{
    Response AddResponse(string value, Question question, Participant participant);
    IEnumerable<Response> GetAllResponses();
    IEnumerable<Response> GetResponsesByProjectId(int projectId);
}
