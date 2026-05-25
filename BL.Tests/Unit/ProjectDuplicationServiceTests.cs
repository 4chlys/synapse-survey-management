using SynapseEnqueteManagement.BL.Domain.I18n.Entities;

namespace SynapseEnqueteManagement.BL.Tests.Unit;

public class ProjectDuplicationServiceTests
{
    private readonly Mock<IEmojiRepository> _emojiRepository = new(MockBehavior.Strict);
    private readonly ProjectDuplicationService _service;

    private static readonly Language LanguageNl = new() { Code = "nl", DisplayName = "Nederlands", IsActive = true };
    private static readonly Language LanguageEn = new() { Code = "en", DisplayName = "English", IsActive = true };

    private static readonly IReadOnlyDictionary<string, Language> Languages =
        new Dictionary<string, Language> { ["nl"] = LanguageNl, ["en"] = LanguageEn };

    public ProjectDuplicationServiceTests()
    {
        _service = new ProjectDuplicationService(_emojiRepository.Object);
    }

    private static ProjectTranslationInput[] Translations(params (string code, string name)[] entries) =>
        entries.Select(e => new ProjectTranslationInput(e.code, e.name, "T")).ToArray();

    [Fact]
    public async Task BuildCloneAsync_HappyPath_DeepClonesGraphAndTranslations()
    {
        // Arrange
        const int platformId = 5;
        const int emojiId = 42;

        var srcSection = new Section
        {
            SectionId = 1,
            Translations = new List<SectionTranslation>
            {
                new() { Language = LanguageNl, Name = "Sectie 1", Description = "desc" }
            }
        };
        var srcParent = new Question
        {
            QuestionId = 101, QuestionFormat = QuestionFormat.Open, IsMandatory = true,
            AnswerChoiceCount = 0, Position = 1,
            Translations = new List<QuestionTranslation>
            {
                new() { Language = LanguageNl, Text = "Parent?", AnswerChoices = new List<string>() }
            }
        };
        var srcChild = new Question
        {
            QuestionId = 102, QuestionFormat = QuestionFormat.MultipleChoice, IsMandatory = false,
            AnswerChoiceCount = 2, ChoiceIndex = 0, Position = 2, Section = srcSection, ParentQuestion = srcParent,
            Translations = new List<QuestionTranslation>
            {
                new() { Language = LanguageNl, Text = "Child?", AnswerChoices = new List<string> { "A", "B" } }
            }
        };
        srcChild.EndingAnswers.Add(new EndingAnswer
        {
            Translations = new List<EndingAnswerTranslation>
            {
                new() { Language = LanguageNl, Value = "end" }
            }
        });

        var srcEmoji = new Emoji { EmojiId = emojiId, Symbol = "❤️", Name = "heart" };
        var srcCat   = new Category
        {
            CategoryId = 9,
            Translations = new List<CategoryTranslation>
            {
                new() { Language = LanguageNl, Name = "Cat A", Description = "cat desc" },
            }
        };

        var source = new Project
        {
            ProjectId = 10,
            ProjectPlatformId = 1,
            ShortForm = "source",
            ImageUrl = "img.png",
            FormType = FormType.VerticalScroll,
            IdeaPageSize = 5,
            MaxExtraIdeaRequests = 3,
            CommentPageSize = 20,
            DefaultLanguage = LanguageNl,
            SupportedLanguages = new List<Language> { LanguageNl, LanguageEn },
            Introduction = new Introduction
            {
                Translations = new List<IntroductionTranslation>
                {
                    new() { Language = LanguageNl, Text = "Intro text" },
                }
            },
            Questionnaire = new Questionnaire
            {
                Sections = new List<Section> { srcSection },
                Questions = new List<Question> { srcParent, srcChild },
            },
            Categories = new List<Category> { srcCat },
            Emojis = new List<Emoji> { srcEmoji },
        };

        var trackedEmoji = new Emoji { EmojiId = emojiId, Symbol = "❤️", Name = "heart" };
        _emojiRepository.Setup(r => r.ReadByIdForUpdateAsync(emojiId)).ReturnsAsync(trackedEmoji);

        // Act
        var result = await _service.BuildCloneAsync(
            source,
            Translations(("nl", "New Project"), ("en", "New Project EN")),
            Languages,
            "new-project",
            platformId);

        // Assert — primitive carry-over
        Assert.Equal("new-project", result.ShortForm);
        Assert.Equal(platformId, result.ProjectPlatformId);
        Assert.Equal(ProjectState.Draft, result.ProjectState);
        Assert.Equal(0, result.ViewCount);
        Assert.Equal(source.ImageUrl, result.ImageUrl);
        Assert.Equal(source.FormType, result.FormType);
        Assert.Equal(source.CommentPageSize, result.CommentPageSize);

        // Introduction
        Assert.NotNull(result.Introduction);
        Assert.Single(result.Introduction.Translations);
        Assert.Equal("Intro text", result.Introduction.Translations.First().Text);

        // Project translations come from inputs, not the source
        Assert.Equal(2, result.Translations.Count);
        Assert.Equal("New Project", result.Translations.First(t => t.Language.Code == "nl").Name);
        Assert.Equal("New Project EN", result.Translations.First(t => t.Language.Code == "en").Name);

        // Questionnaire
        Assert.Single(result.Questionnaire.Sections);
        Assert.Equal("Sectie 1", result.Questionnaire.Sections.First().Translations.First(t => t.Language.Code == "nl").Name);

        Assert.Equal(2, result.Questionnaire.Questions.Count);
        var newParent = result.Questionnaire.Questions.First(q => q.Position == 1);
        var newChild  = result.Questionnaire.Questions.First(q => q.Position == 2);

        // Parent-child remap: child's ParentQuestion must point to the NEW parent, not the source
        Assert.Same(newParent, newChild.ParentQuestion);
        Assert.NotSame(srcParent, newChild.ParentQuestion);

        Assert.Equal(0, newChild.ChoiceIndex);
        Assert.Single(newChild.EndingAnswers);
        Assert.Equal("end", newChild.EndingAnswers.First().Translations.First().Value);
        Assert.Same(result.Questionnaire.Sections.First(), newChild.Section);

        // Categories
        Assert.Single(result.Categories);
        Assert.Equal("Cat A", result.Categories.First().Translations.First().Name);

        // Emojis attach via the repository (tracked instances), not the source's detached copies
        Assert.Single(result.Emojis);
        Assert.Same(trackedEmoji, result.Emojis.First());
    }

    [Fact]
    public async Task BuildCloneAsync_PropagatesAllowCrossLanguageIdeas()
    {
        var source = new Project
        {
            ProjectId = 20,
            ProjectPlatformId = 1,
            ShortForm = "source",
            AllowCrossLanguageIdeas = true,
            DefaultLanguage = LanguageNl,
            SupportedLanguages = new List<Language> { LanguageNl },
            Questionnaire = new Questionnaire(),
            Categories = new List<Category>(),
            Emojis = new List<Emoji>(),
        };

        var result = await _service.BuildCloneAsync(
            source,
            Translations(("nl", "Clone Me")),
            Languages,
            "clone-me",
            5);

        Assert.True(result.AllowCrossLanguageIdeas);
    }

    [Fact]
    public async Task BuildCloneAsync_NullDefaultLanguageOnSource_Throws()
    {
        var source = new Project
        {
            ProjectId = 30,
            DefaultLanguage = null,
            SupportedLanguages = new List<Language>(),
            Questionnaire = new Questionnaire(),
            Categories = new List<Category>(),
            Emojis = new List<Emoji>(),
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.BuildCloneAsync(source, Translations(("nl", "X")), Languages, "x", 1));
    }
}
