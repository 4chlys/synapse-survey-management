namespace SynapseEnqueteManagement.UI.Admin.Models.Category;

public record CategoryFormViewModel
{
    public int CategoryId { get; init; }
    public ICollection<CategoryTranslationViewModel> Translations { get; init; } = new List<CategoryTranslationViewModel>();
}
