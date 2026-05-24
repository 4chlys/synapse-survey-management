using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.Extensions.Localization;
using SynapseEnqueteManagement.API.DTOs.Project;
using SynapseEnqueteManagement.API.Services;
using SynapseEnqueteManagement.BL;
using SynapseEnqueteManagement.BL.AiImageGeneration;
using SynapseEnqueteManagement.BL.Domain.I18n.Entities;

namespace SynapseEnqueteManagement.API.Tests;

public class ProjectsControllerTests
{
    private readonly Mock<IProjectManager> _projectManager = new();
    private readonly Mock<IAiImageGenerationService> _imageGenerationService = new();
    private readonly Mock<IStorageService> _storageService = new();
    private readonly ProjectsController _controller;

    private static readonly Language LanguageNl = new() { Code = "nl", DisplayName = "Nederlands", IsActive = true };
    private static readonly Language LanguageEn = new() { Code = "en", DisplayName = "English", IsActive = true };

    public ProjectsControllerTests()
    {
        _imageGenerationService
            .Setup(s => s.GenerateProjectImageAsync(
                It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ImageGenerationResult.Failed("disabled"));

        _controller = new ProjectsController(
            _projectManager.Object,
            _imageGenerationService.Object,
            _storageService.Object);
    }

    private static Project BilingualProject(int id = 1, string defaultCode = "nl") => new()
    {
        ProjectId = id,
        ShortForm = "demo",
        ProjectState = ProjectState.Active,
        DefaultLanguage = defaultCode == "nl" ? LanguageNl : LanguageEn,
        SupportedLanguages = new List<Language> { LanguageNl, LanguageEn },
        Translations = new List<ProjectTranslation>
        {
            new() { Language = LanguageNl, Name = "Naam NL", Topic = "Onderwerp NL" },
            new() { Language = LanguageEn, Name = "Name EN", Topic = "Topic EN" },
        },
    };

    #region GetByShortForm

    [Fact]
    public async Task GetByShortForm_Found_ReturnsOkWithLookupDto()
    {
        // Arrange
        var project = new Project
        {
            ProjectId = 5,
            ShortForm = "demo",
            ProjectState = ProjectState.Active
        };
        _projectManager.Setup(m => m.GetProjectByShortFormAsync("demo")).ReturnsAsync(project);

        // Act
        var result = await _controller.GetByShortForm("demo");

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ProjectShortFormLookupDto>(ok.Value);
        Assert.Equal(5, dto.ProjectId);
        Assert.Equal("demo", dto.ShortForm);
        Assert.Equal("Active", dto.ProjectState);
    }

    [Fact]
    public async Task GetByShortForm_NotFound_Returns404()
    {
        // Arrange
        _projectManager.Setup(m => m.GetProjectByShortFormAsync("unknown")).ReturnsAsync((Project)null);

        // Act
        var result = await _controller.GetByShortForm("unknown");

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetByShortForm_LowercasesInput()
    {
        // Arrange
        _projectManager.Setup(m => m.GetProjectByShortFormAsync("demo")).ReturnsAsync((Project)null);

        // Act
        await _controller.GetByShortForm("DEMO");

        // Assert
        _projectManager.Verify(m => m.GetProjectByShortFormAsync("demo"), Times.Once);
    }

    #endregion

    #region GetProject — culture resolution

    [Fact]
    public async Task GetProject_ActiveCultureEnglish_ResolvesEnglishTranslation()
    {
        // Arrange
        var project = BilingualProject();
        _projectManager.Setup(m => m.GetProjectByIdWithIntroductionReadOnlyAsync(1)).ReturnsAsync(project);
        var previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = new CultureInfo("en");

        try
        {
            // Act
            var result = await _controller.GetProject(1);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<ProjectDto>(ok.Value);
            Assert.Equal("Name EN", dto.Name);
            Assert.Equal("Topic EN", dto.Topic);
            Assert.Equal("en", dto.LanguageCode);
            Assert.Equal("nl", dto.DefaultLanguageCode);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public async Task GetProject_UnsupportedCulture_FallsBackToDefaultLanguage()
    {
        // Arrange
        var project = BilingualProject();
        _projectManager.Setup(m => m.GetProjectByIdWithIntroductionReadOnlyAsync(1)).ReturnsAsync(project);
        var previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = new CultureInfo("fr");

        try
        {
            // Act
            var result = await _controller.GetProject(1);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<ProjectDto>(ok.Value);
            Assert.Equal("Naam NL", dto.Name);
            Assert.Equal("nl", dto.LanguageCode);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    #endregion

    #region CreateProject — admin DTO shape

    [Fact]
    public async Task CreateProject_ReturnsAdminDto_WithAllTranslations()
    {
        // Arrange
        var created = BilingualProject(id: 7);
        _projectManager
            .Setup(m => m.CreateProjectAsync(
                It.IsAny<IEnumerable<ProjectTranslationInput>>(),
                It.IsAny<IEnumerable<IntroductionTranslationInput>>(),
                "nl",
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string>(), It.IsAny<FormType>(), It.IsAny<AiAssistanceType>(),
                It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<bool>(),
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<bool>(),
                It.IsAny<ProjectTheme>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(created);

        var dto = new CreateProjectDto
        {
            DefaultLanguageCode = "nl",
            SupportedLanguageCodes = new[] { "nl", "en" },
            Translations = new List<ProjectTranslationDto>
            {
                new() { LanguageCode = "nl", Name = "Naam NL" },
                new() { LanguageCode = "en", Name = "Name EN" },
            },
            PlatformId = 1,
        };

        // Act
        var result = await _controller.CreateProject(dto, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedResult>(result.Result);
        var adminDto = Assert.IsType<AdminProjectDto>(createdResult.Value);
        Assert.Equal(2, adminDto.Translations.Count);
        Assert.Equal("nl", adminDto.DefaultLanguageCode);
        Assert.Contains(adminDto.Translations, t => t.LanguageCode == "nl" && t.Name == "Naam NL");
        Assert.Contains(adminDto.Translations, t => t.LanguageCode == "en" && t.Name == "Name EN");
    }

    [Fact]
    public async Task CreateProject_AllowCrossLanguageIdeas_PassedThroughToManager()
    {
        // Arrange — setup only matches when the flag is true, verifying pass-through
        var created = BilingualProject(id: 8);
        created.AllowCrossLanguageIdeas = true;
        _projectManager
            .Setup(m => m.CreateProjectAsync(
                It.IsAny<IEnumerable<ProjectTranslationInput>>(),
                It.IsAny<IEnumerable<IntroductionTranslationInput>>(),
                It.IsAny<string>(), It.IsAny<IEnumerable<string>>(),
                It.IsAny<string>(), It.IsAny<FormType>(), It.IsAny<AiAssistanceType>(),
                It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<bool>(),
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<bool>(), It.IsAny<int>(), true,
                It.IsAny<ProjectTheme>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(created);

        var dto = new CreateProjectDto
        {
            DefaultLanguageCode = "nl",
            SupportedLanguageCodes = new[] { "nl", "en" },
            Translations = new List<ProjectTranslationDto> { new() { LanguageCode = "nl", Name = "X" }, new() { LanguageCode = "en", Name = "Y" } },
            PlatformId = 1,
            AllowCrossLanguageIdeas = true,
        };

        // Act
        var result = await _controller.CreateProject(dto, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedResult>(result.Result);
        var adminDto = Assert.IsType<AdminProjectDto>(createdResult.Value);
        Assert.True(adminDto.AllowCrossLanguageIdeas);
        _projectManager.Verify(m => m.CreateProjectAsync(
            It.IsAny<IEnumerable<ProjectTranslationInput>>(),
            It.IsAny<IEnumerable<IntroductionTranslationInput>>(),
            It.IsAny<string>(), It.IsAny<IEnumerable<string>>(),
            It.IsAny<string>(), It.IsAny<FormType>(), It.IsAny<AiAssistanceType>(),
            It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<bool>(),
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<bool>(), It.IsAny<int>(), true,
            It.IsAny<ProjectTheme>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task CreateProject_ReservedShortForm_ManagerThrows_Propagates()
    {
        // Arrange
        _projectManager
            .Setup(m => m.CreateProjectAsync(
                It.IsAny<IEnumerable<ProjectTranslationInput>>(),
                It.IsAny<IEnumerable<IntroductionTranslationInput>>(),
                It.IsAny<string>(), It.IsAny<IEnumerable<string>>(),
                It.IsAny<string>(), It.IsAny<FormType>(), It.IsAny<AiAssistanceType>(),
                It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<bool>(),
                It.IsAny<int>(), "admin", It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<bool>(),
                It.IsAny<ProjectTheme>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new ValidationException("'admin' is a reserved shortform."));

        var dto = new CreateProjectDto
        {
            DefaultLanguageCode = "nl",
            SupportedLanguageCodes = new[] { "nl" },
            Translations = new List<ProjectTranslationDto>
            {
                new() { LanguageCode = "nl", Name = "Admin" },
            },
            ShortForm = "admin",
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _controller.CreateProject(dto, CancellationToken.None));
    }

    #endregion

    #region UpdateProject — ShortForm locking

    [Fact]
    public async Task UpdateProject_AllowCrossLanguageIdeas_PassedThroughToManager()
    {
        // Arrange — setup only matches when the flag is true, verifying pass-through
        var updated = BilingualProject(id: 1);
        updated.AllowCrossLanguageIdeas = true;
        _projectManager
            .Setup(m => m.UpdateProjectAsync(
                1,
                It.IsAny<IEnumerable<ProjectTranslationInput>>(),
                It.IsAny<IEnumerable<IntroductionTranslationInput>>(),
                It.IsAny<string>(), It.IsAny<IEnumerable<string>>(),
                It.IsAny<string>(), It.IsAny<FormType>(), It.IsAny<AiAssistanceType>(),
                It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<bool>(),
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<int>(), true,
                It.IsAny<ProjectTheme>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(updated);

        var dto = new UpdateProjectDto
        {
            DefaultLanguageCode = "nl",
            SupportedLanguageCodes = new[] { "nl", "en" },
            Translations = new List<ProjectTranslationDto> { new() { LanguageCode = "nl", Name = "Naam" }, new() { LanguageCode = "en", Name = "Name" } },
            AllowCrossLanguageIdeas = true,
        };

        // Act
        var result = await _controller.UpdateProject(1, dto);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var adminDto = Assert.IsType<AdminProjectDto>(ok.Value);
        Assert.True(adminDto.AllowCrossLanguageIdeas);
        _projectManager.Verify(m => m.UpdateProjectAsync(
            1,
            It.IsAny<IEnumerable<ProjectTranslationInput>>(),
            It.IsAny<IEnumerable<IntroductionTranslationInput>>(),
            It.IsAny<string>(), It.IsAny<IEnumerable<string>>(),
            It.IsAny<string>(), It.IsAny<FormType>(), It.IsAny<AiAssistanceType>(),
            It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<bool>(),
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<int>(), true,
            It.IsAny<ProjectTheme>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task UpdateProject_ShortFormChangeInActive_ManagerThrows_Propagates()
    {
        // Arrange
        _projectManager
            .Setup(m => m.UpdateProjectAsync(
                1,
                It.IsAny<IEnumerable<ProjectTranslationInput>>(),
                It.IsAny<IEnumerable<IntroductionTranslationInput>>(),
                It.IsAny<string>(), It.IsAny<IEnumerable<string>>(),
                It.IsAny<string>(), It.IsAny<FormType>(), It.IsAny<AiAssistanceType>(),
                It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<bool>(),
                "new-form", It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<bool>(),
                It.IsAny<ProjectTheme>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new ValidationException("ShortForm cannot be changed after the project has been published."));

        var dto = new UpdateProjectDto
        {
            ShortForm = "new-form",
            DefaultLanguageCode = "nl",
            SupportedLanguageCodes = new[] { "nl" },
            Translations = new List<ProjectTranslationDto>
            {
                new() { LanguageCode = "nl", Name = "Naam" },
            },
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _controller.UpdateProject(1, dto));
    }

    #endregion
}
