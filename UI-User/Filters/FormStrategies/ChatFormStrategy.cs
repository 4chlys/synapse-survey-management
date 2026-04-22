using SynapseEnqueteManagement.UI.User.Filters.FormStrategies;
namespace SynapseEnqueteManagement.UI.User.Filters.FormStrategies;

public class ChatFormStrategy : IFormStrategy
{
    public string FormType => "Chat";

    public string GetViewName() => "Chat";

    public FormLayoutData GetLayoutData() => new()
    {
        ContainerCssClass = "chat-form",
        JsModule = "chat"
    };
}