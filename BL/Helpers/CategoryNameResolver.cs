using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.BL.Helpers;

// Resolves a category's display name for a language, falling back to the first available
// translation and then to empty. Shared by the dashboard feed and moderation listing mappers.
public static class CategoryNameResolver
{
    public static string Resolve(Category category, string languageCode)
        => TranslationPicker.Pick(category?.Translations, languageCode)?.Name ?? string.Empty;
}
