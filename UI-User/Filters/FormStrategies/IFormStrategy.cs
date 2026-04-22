using SynapseEnqueteManagement.UI.User.Filters.FormStrategies;
namespace SynapseEnqueteManagement.UI.User.Filters.FormStrategies;

public interface IFormStrategy
{
    string FormType { get; }
    string GetViewName();
    FormLayoutData GetLayoutData();
}