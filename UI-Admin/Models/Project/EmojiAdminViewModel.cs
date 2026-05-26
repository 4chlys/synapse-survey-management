namespace SynapseEnqueteManagement.UI.Admin.Models.Project;

public record EmojiAdminViewModel
{
    public int EmojiId { get; init; }
    public string Symbol { get; init; }
    public string Name { get; init; }
    public bool IsEnabled { get; init; }
}
