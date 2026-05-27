using SynapseEnqueteManagement.BL.Domain.I18n.Entities;

namespace SynapseEnqueteManagement.BL.Tests.Unit;

public class SectionManagerTests
{
    private readonly Mock<ISectionRepository> _sectionRepo = new(MockBehavior.Strict);
    private readonly Mock<IQuestionnaireRepository> _questionnaireRepo = new(MockBehavior.Strict);
    private readonly Mock<ILanguageResolver> _languageResolver = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _uow = new(MockBehavior.Strict);
    private readonly SectionManager _manager;

    private const int ProjectId = 1;
    private const int SectionId = 5;

    private readonly Questionnaire _questionnaire = new()
    {
        QuestionnaireId = 99,
        Project = new Project { ProjectId = ProjectId }
    };

    public SectionManagerTests()
    {
        _manager = new SectionManager(
            _sectionRepo.Object,
            _questionnaireRepo.Object,
            _languageResolver.Object,
            _uow.Object);
    }

    private void SetupLanguages(params string[] codes)
        => _languageResolver.Setup(r => r.ResolveForTranslationsAsync(It.IsAny<ICollection<string>>(), true))
            .ReturnsAsync((IReadOnlyDictionary<string, Language>)codes.ToDictionary(c => c, c => new Language { Code = c }));

    [Fact]
    public async Task GetSectionsReadOnlyAsync_DelegatesToRepository()
    {
        var sections = new List<Section> { new() { SectionId = SectionId } };
        _sectionRepo.Setup(r => r.ReadByProjectIdWithTranslationsReadOnlyAsync(ProjectId)).ReturnsAsync(sections);

        var result = await _manager.GetSectionsReadOnlyAsync(ProjectId);

        Assert.Same(sections, result);
    }

    [Fact]
    public async Task CreateSectionAsync_QuestionnaireNotFound_ReturnsNull()
    {
        _questionnaireRepo.Setup(r => r.ReadByProjectIdForUpdateAsync(ProjectId))
            .ReturnsAsync((Questionnaire)null);

        var result = await _manager.CreateSectionAsync(ProjectId,
            new[] { new SectionTranslationInput("nl", "Intro", "Beschrijving") });

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateSectionAsync_AttachesParentQuestionnaireAndPersists()
    {
        _questionnaireRepo.Setup(r => r.ReadByProjectIdForUpdateAsync(ProjectId))
            .ReturnsAsync(_questionnaire);
        SetupLanguages("nl");
        _sectionRepo.Setup(r => r.CreateAsync(It.IsAny<Section>())).ReturnsAsync((Section s) => s);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _manager.CreateSectionAsync(ProjectId,
            new[] { new SectionTranslationInput("nl", "Intro", "Beschrijving") });

        Assert.NotNull(result);
        Assert.Same(_questionnaire, result.Questionnaire);
        var translation = Assert.Single(result.Translations);
        Assert.Equal("Intro", translation.Name);
        Assert.Equal("Beschrijving", translation.Description);
        Assert.Equal("nl", translation.Language.Code);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateSectionAsync_SectionNotFound_ReturnsFalse()
    {
        _sectionRepo.Setup(r => r.ReadByIdForUpdateAsync(SectionId))
            .ReturnsAsync((Section)null);

        var result = await _manager.UpdateSectionAsync(SectionId,
            new[] { new SectionTranslationInput("nl", "Intro", "Beschrijving") });

        Assert.False(result);
    }

    [Fact]
    public async Task UpdateSectionAsync_UpsertsTranslationsAndPersists()
    {
        var nl = new Language { Code = "nl" };
        var section = new Section
        {
            SectionId = SectionId,
            Translations = new List<SectionTranslation>
            {
                new() { Language = nl, Name = "Oud", Description = "Oude beschrijving" }
            }
        };
        _sectionRepo.Setup(r => r.ReadByIdForUpdateAsync(SectionId)).ReturnsAsync(section);
        SetupLanguages("nl");
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _manager.UpdateSectionAsync(SectionId,
            new[] { new SectionTranslationInput("nl", "Nieuw", "Nieuwe beschrijving") });

        Assert.True(result);
        var translation = Assert.Single(section.Translations);
        Assert.Equal("Nieuw", translation.Name);
        Assert.Equal("Nieuwe beschrijving", translation.Description);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteSectionAsync_RunsDeleteInTransaction()
    {
        _uow.Setup(u => u.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<bool>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task<bool>> work, CancellationToken ct) => work(ct));
        _sectionRepo.Setup(r => r.DeleteByIdAsync(SectionId)).ReturnsAsync(true);

        var result = await _manager.DeleteSectionAsync(SectionId);

        Assert.True(result);
        _sectionRepo.Verify(r => r.DeleteByIdAsync(SectionId), Times.Once);
    }
}
