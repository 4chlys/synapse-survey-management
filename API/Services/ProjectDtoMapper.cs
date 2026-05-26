using System.Globalization;
using SynapseEnqueteManagement.API.DTOs.Project;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.API.Services;

public static class ProjectDtoMapper
{
    public static ProjectDto ToParticipantDto(Project p)
    {
        var t = ResolveTranslation(p);
        var active = ActiveCulture();
        var introText = p.Introduction?.Translations
                            .FirstOrDefault(it => it.Language?.Code == active)?.Text
                        ?? p.Introduction?.Translations
                            .FirstOrDefault(it => it.Language?.Code == p.DefaultLanguage?.Code)?.Text
                        ?? p.Introduction?.Translations.FirstOrDefault()?.Text
                        ?? string.Empty;
        return new ProjectDto
        {
            ProjectId = p.ProjectId,
            PlatformId = p.ProjectPlatformId,
            Name = t?.Name ?? string.Empty,
            ShortForm = p.ShortForm ?? string.Empty,
            Topic = t?.Topic ?? string.Empty,
            ProjectState = p.ProjectState.ToString(),
            FormType = p.FormType.ToString(),
            OrganisationName = p.ProjectPlatform?.Name ?? string.Empty,
            ProjectImage = p.ImageUrl ?? string.Empty,
            IsAiGeneratedImage = IsAiGeneratedImageUrl(p.ImageUrl),
            OrganisationLogoUrl = p.ProjectPlatform?.LogoUrl ?? string.Empty,
            OrganisationWebsiteUrl = p.ProjectPlatform?.WebsiteUrl ?? string.Empty,
            Introduction = introText,
            RequestContactInfo = p.RequestContactInfo,
            AllowCrossLanguageIdeas = p.AllowCrossLanguageIdeas,
            IdeaPageSize = p.IdeaPageSize,
            ExtraIdeasReplaceExisting = p.ExtraIdeasReplaceExisting,
            MaxExtraIdeaRequests = p.MaxExtraIdeaRequests,
            MinAge = p.MinAge,
            MaxAge = p.MaxAge,
            LanguageCode = t?.Language?.Code ?? p.DefaultLanguage?.Code,
            DefaultLanguageCode = p.DefaultLanguage?.Code,
            SupportedLanguageCodes = p.SupportedLanguages.Select(l => l.Code).ToList(),
            Translations = p.Translations.Select(pt => new ProjectTranslationDto
            {
                LanguageCode = pt.Language?.Code,
                Name = pt.Name,
                Topic = pt.Topic,
                Introduction = p.Introduction?.Translations
                    .FirstOrDefault(it => it.Language?.Code == pt.Language?.Code)?.Text,
            }).ToList(),
            LastWizardStep = p.LastWizardStep,
            Theme = p.Theme.ToString(),
            PrimaryColor = p.PrimaryColor,
            CustomCss = p.CustomCss,
        };
    }

    public static AdminProjectDto ToAdminDto(Project p) => new()
    {
        ProjectId = p.ProjectId,
        PlatformId = p.ProjectPlatformId,
        ShortForm = p.ShortForm ?? string.Empty,
        ProjectState = p.ProjectState.ToString(),
        FormType = p.FormType.ToString(),
        OrganisationName = p.ProjectPlatform?.Name ?? string.Empty,
        ProjectImage = p.ImageUrl ?? string.Empty,
        IsAiGeneratedImage = IsAiGeneratedImageUrl(p.ImageUrl),
        OrganisationLogoUrl = p.ProjectPlatform?.LogoUrl ?? string.Empty,
        OrganisationWebsiteUrl = p.ProjectPlatform?.WebsiteUrl ?? string.Empty,
        RequestContactInfo = p.RequestContactInfo,
        AllowCrossLanguageIdeas = p.AllowCrossLanguageIdeas,
        IdeaPageSize = p.IdeaPageSize,
        ExtraIdeasReplaceExisting = p.ExtraIdeasReplaceExisting,
        MaxExtraIdeaRequests = p.MaxExtraIdeaRequests,
        MinAge = p.MinAge,
        MaxAge = p.MaxAge,
        DefaultLanguageCode = p.DefaultLanguage?.Code,
        SupportedLanguageCodes = p.SupportedLanguages.Select(l => l.Code).ToList(),
        Translations = p.Translations.Select(t => new ProjectTranslationDto
        {
            LanguageCode = t.Language?.Code,
            Name = t.Name,
            Topic = t.Topic,
            Introduction = p.Introduction?.Translations
                .FirstOrDefault(it => it.Language?.Code == t.Language?.Code)?.Text,
        }).ToList(),
        LastWizardStep = p.LastWizardStep,
        Theme = p.Theme.ToString(),
        PrimaryColor = p.PrimaryColor,
        CustomCss = p.CustomCss,
    };

    private static string ActiveCulture() =>
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

    private static ProjectTranslation ResolveTranslation(Project p)
    {
        var active = ActiveCulture();
        return p.Translations.FirstOrDefault(t => t.Language?.Code == active)
               ?? p.Translations.FirstOrDefault(t => t.Language?.Code == p.DefaultLanguage?.Code)
               ?? p.Translations.FirstOrDefault();
    }

    private static bool IsAiGeneratedImageUrl(string imageUrl) =>
        !string.IsNullOrWhiteSpace(imageUrl)
        && Path.GetFileNameWithoutExtension(imageUrl).Contains("generated", StringComparison.OrdinalIgnoreCase);
}
