using System.Globalization;
using SynapseEnqueteManagement.API.Services;
using SynapseEnqueteManagement.BL.Domain.I18n.Entities;
using SynapseEnqueteManagement.BL.Domain.Platform.Entities;

namespace SynapseEnqueteManagement.API.Tests.Unit;

public class ProjectDtoMapperTests
{
    private static Language Lang(string code) => new() { Code = code, DisplayName = code };

    private static Project BuildProject() => new()
    {
        ProjectId = 7,
        ProjectPlatformId = 3,
        ProjectPlatform = new ProjectPlatform { Name = "Org", LogoUrl = "logo.png", WebsiteUrl = "https://org" },
        ShortForm = "sf",
        DefaultLanguage = Lang("nl"),
        SupportedLanguages = new List<Language> { Lang("nl"), Lang("en") },
        Translations = new List<ProjectTranslation>
        {
            new() { Language = Lang("nl"), Name = "Naam", Topic = "Onderwerp" },
            new() { Language = Lang("en"), Name = "Name", Topic = "Topic" },
        },
        ImageUrl = "projects/photo.png",
        ProjectState = ProjectState.Active,
        Theme = ProjectTheme.Minimal,
    };

    private static void WithCulture(string code, Action body)
    {
        var original = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(code);
            body();
        }
        finally { CultureInfo.CurrentUICulture = original; }
    }

    [Fact]
    public void ToParticipantDto_PicksTranslationMatchingActiveCulture()
        => WithCulture("en", () =>
        {
            var dto = ProjectDtoMapper.ToParticipantDto(BuildProject());
            Assert.Equal("Name", dto.Name);
            Assert.Equal("Topic", dto.Topic);
            Assert.Equal("en", dto.LanguageCode);
        });

    [Fact]
    public void ToParticipantDto_FallsBackToDefaultLanguage_WhenActiveCultureMissing()
        => WithCulture("fr", () =>
        {
            var dto = ProjectDtoMapper.ToParticipantDto(BuildProject());
            Assert.Equal("Naam", dto.Name);
            Assert.Equal("nl", dto.LanguageCode);
        });

    [Fact]
    public void ToParticipantDto_FlattensPlatformAndScalarFields()
        => WithCulture("nl", () =>
        {
            var dto = ProjectDtoMapper.ToParticipantDto(BuildProject());
            Assert.Equal(7, dto.ProjectId);
            Assert.Equal(3, dto.PlatformId);
            Assert.Equal("Org", dto.OrganisationName);
            Assert.Equal("logo.png", dto.OrganisationLogoUrl);
            Assert.Equal("Active", dto.ProjectState);
            Assert.Equal(new[] { "nl", "en" }, dto.SupportedLanguageCodes);
        });

    [Theory]
    [InlineData("projects/generated.png", true)]
    [InlineData("projects/Generated123.jpg", true)]
    [InlineData("projects/photo.png", false)]
    [InlineData("", false)]
    public void ToParticipantDto_DetectsAiGeneratedImageByFilename(string imageUrl, bool expected)
        => WithCulture("nl", () =>
        {
            var p = BuildProject();
            p.ImageUrl = imageUrl;
            Assert.Equal(expected, ProjectDtoMapper.ToParticipantDto(p).IsAiGeneratedImage);
        });

    [Fact]
    public void ToParticipantDto_ResolvesIntroductionForActiveCulture()
        => WithCulture("en", () =>
        {
            var p = BuildProject();
            p.Introduction = new Introduction
            {
                Translations = new List<IntroductionTranslation>
                {
                    new() { Language = Lang("nl"), Text = "Hallo" },
                    new() { Language = Lang("en"), Text = "Hello" },
                }
            };
            Assert.Equal("Hello", ProjectDtoMapper.ToParticipantDto(p).Introduction);
        });

    [Fact]
    public void ToAdminDto_EmitsAllTranslationsWithMatchingIntroduction()
        => WithCulture("nl", () =>
        {
            var p = BuildProject();
            p.Introduction = new Introduction
            {
                Translations = new List<IntroductionTranslation>
                {
                    new() { Language = Lang("en"), Text = "Hello" },
                }
            };

            var dto = ProjectDtoMapper.ToAdminDto(p);

            Assert.Equal(2, dto.Translations.Count);
            var en = dto.Translations.Single(t => t.LanguageCode == "en");
            Assert.Equal("Name", en.Name);
            Assert.Equal("Hello", en.Introduction);
            Assert.Null(dto.Translations.Single(t => t.LanguageCode == "nl").Introduction);
        });
}
