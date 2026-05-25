namespace SynapseEnqueteManagement.UI.User.Models;

public record ProjectShortFormLookupViewModel
{
    public int ProjectId { get; init; }
    public string ShortForm { get; init; }
    public string ProjectState { get; init; }
}
