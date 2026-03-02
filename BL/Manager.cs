using SynapseEnqueteManagement.DAL;

namespace SynapseEnqueteManagement.BL;

public class Manager : IManager
{
    private readonly IRepository _repository;
    
    public Manager(IRepository repository)
    {
        _repository = repository;
    }
    
}