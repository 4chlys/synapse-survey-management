namespace SynapseEnqueteManagement.UI.User.FormStrategies;

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
