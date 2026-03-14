namespace SynapseEnqueteManagement.UI.User.FormStrategies;

public interface IFormStrategy
{
    string FormType { get; }
    string GetViewName();
    FormLayoutData GetLayoutData();
}