namespace SynapseEnqueteManagement.UI.Admin.Models.Section;

public record SectionViewModel
{
    public int SectionId { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
    public ICollection<SectionTranslationViewModel> Translations { get; init; } = new List<SectionTranslationViewModel>();
}
