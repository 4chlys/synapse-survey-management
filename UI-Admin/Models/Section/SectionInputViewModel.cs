namespace SynapseEnqueteManagement.UI.Admin.Models.Section;

public record SectionInputViewModel
{
    public IList<SectionTranslationViewModel> Translations { get; init; } = new List<SectionTranslationViewModel>();
}
