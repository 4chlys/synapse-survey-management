namespace SynapseEnqueteManagement.UI.User.FormStrategies;

public class FormStrategyFactory : IFormStrategyFactory
{
    private readonly IEnumerable<IFormStrategy> _strategies;

    public FormStrategyFactory(IEnumerable<IFormStrategy> strategies)
    {
        _strategies = strategies;
    }

    public IFormStrategy Create(string formType) =>
        _strategies.FirstOrDefault(s => s.FormType == formType)
        ?? _strategies.First(s => s.FormType == "VerticalScroll");
}