namespace SynapseEnqueteManagement.UI.User.FormStrategies;

public interface IFormStrategyFactory
{
    IFormStrategy Create(string formType);
}
