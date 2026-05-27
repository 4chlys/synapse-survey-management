namespace SynapseEnqueteManagement.UI.Admin.Models.Category;

public record CategoryTranslationViewModel
{
    public string LanguageCode { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
}
