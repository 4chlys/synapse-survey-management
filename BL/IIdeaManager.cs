using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.BL;

public interface IIdeaManager
{
    IEnumerable<Idea> GetIdeasByCategory(int categoryId);
    Idea AddIdea(int categoryId, string description);

}