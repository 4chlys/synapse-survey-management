using SynapseEnqueteManagement.UI.User.Filters.FormStrategies;
namespace SynapseEnqueteManagement.UI.User.Filters.FormStrategies;

public interface IFormStrategyFactory
{
    IFormStrategy Create(string formType);
}