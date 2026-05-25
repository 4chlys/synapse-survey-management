using SynapseEnqueteManagement.UI.User.Filters.FormStrategies;
namespace SynapseEnqueteManagement.UI.User.Filters.FormStrategies;

public class ScrollFormStrategy : IFormStrategy
{
    public string FormType => "VerticalScroll";

    public string GetViewName() => "Scrollform";

    public FormLayoutData GetLayoutData() => new()
    {
        ContainerCssClass = "scroll-form",
        JsModule = "scroll"
    };
}