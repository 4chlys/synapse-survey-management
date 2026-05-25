namespace SynapseEnqueteManagement.UI.User.Models.Ideation;

public record CreateIdeaResultViewModel
{
    public bool Success { get; init; }
    public bool ShouldAskForContactInfo { get; init; }
}
