namespace SynapseEnqueteManagement.UI.Admin.Models.Section;

public record SectionTranslationViewModel
{
    public string LanguageCode { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
}
