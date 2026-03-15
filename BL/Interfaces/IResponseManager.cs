using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.BL.Interfaces;

public interface IResponseManager
{
    Task<Response> AddResponseAsync(string value, Question question, Participant participant);
}