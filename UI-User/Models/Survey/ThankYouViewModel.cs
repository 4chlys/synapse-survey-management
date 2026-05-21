namespace SynapseEnqueteManagement.UI.User.Models.Survey;

public record ThankYouViewModel
{
    public int ProjectId { get; init; }
    public string OrganisationName { get; init; } = string.Empty;
    public string OrganisationLogoUrl { get; init; } = string.Empty;
    public string OrganisationWebsiteUrl { get; init; } = string.Empty;
}
