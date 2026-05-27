namespace SynapseEnqueteManagement.UI.Admin.Models.Project;

// Minimal data the _EmailBroadcastModal partial needs. A dedicated model so
// the same partial can be rendered from both the ideas list (IdeasViewModel)
// and the category-picker page (IdeasCategoriesViewModel) without coupling
// the partial to either.
public record EmailBroadcastModalModel(
    int ProjectId,
    IReadOnlyList<string> SupportedLanguageCodes,
    string DefaultLanguageCode);
