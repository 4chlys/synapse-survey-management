namespace SynapseEnqueteManagement.UI.User.FormStrategies;

public class ScrollFormStrategy : IFormStrategy
{
    public string FormType => "VerticalScroll";

    public string GetViewName() => "Index";

    public FormLayoutData GetLayoutData() => new()
    {
        ContainerCssClass = "scroll-form",
        JsModule = "scroll"
    };
}
