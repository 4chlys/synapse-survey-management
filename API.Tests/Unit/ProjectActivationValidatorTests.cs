using SynapseEnqueteManagement.BL.Domain.I18n.Entities;
using SynapseEnqueteManagement.BL.Implementation;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.API.Tests;

public class ProjectActivationValidatorTests
{
    private static readonly Language LangNl = new() { Code = "nl", DisplayName = "Nederlands" };
    private static readonly Language LangEn = new() { Code = "en", DisplayName = "English" };

    private readonly ProjectActivationValidator _validator = new();

    private static Project BilingualProject() => new()
    {
        ProjectId = 1,
        SupportedLanguages = new List<Language> { LangNl, LangEn },
        Translations = new List<ProjectTranslation>
        {
            new() { Language = LangNl, Name = "Naam", Topic = "Onderwerp" },
            new() { Language = LangEn, Name = "Name", Topic = "Topic" },
        },
        Categories = new List<Category>
        {
            new() { CategoryId = 1, Translations = new List<CategoryTranslation>
            {
                new() { Language = LangNl, Name = "Cat NL" },
                new() { Language = LangEn, Name = "Cat EN" },
            }},
        },
        Questionnaire = new Questionnaire
        {
            Sections = new List<Section>(),
            Questions = new List<Question>(),
        },
    };

    [Fact]
    public async Task Validate_CompleteProject_ReturnsNoProblems()
    {
        var project = BilingualProject();

        var problems = _validator.Validate(project);

        Assert.Empty(problems);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Validate_MissingProjectNameTranslation_ReturnsProblem()
    {
        var project = BilingualProject();
        project.Translations = new List<ProjectTranslation>
        {
            new() { Language = LangNl, Name = "Naam", Topic = "Onderwerp" },
            // English translation missing
        };

        var problems = _validator.Validate(project);

        Assert.Single(problems);
        Assert.Equal("ProjectTranslation", problems[0].EntityType);
        Assert.Equal("en", problems[0].LanguageCode);
        Assert.Null(problems[0].EntityId);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Validate_MissingSectionTranslation_ReturnsProblem()
    {
        var project = BilingualProject();
        project.Questionnaire.Sections = new List<Section>
        {
            new()
            {
                SectionId = 5,
                Translations = new List<SectionTranslation>
                {
                    new() { Language = LangNl, Name = "Sectie NL" },
                    // English section translation missing
                },
            },
        };

        var problems = _validator.Validate(project);

        Assert.Single(problems);
        Assert.Equal("SectionTranslation", problems[0].EntityType);
        Assert.Equal(5, problems[0].EntityId);
        Assert.Equal("en", problems[0].LanguageCode);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Validate_MissingQuestionTranslation_ReturnsProblem()
    {
        var project = BilingualProject();
        project.Questionnaire.Questions = new List<Question>
        {
            new()
            {
                QuestionId = 12,
                Translations = new List<QuestionTranslation>
                {
                    new() { Language = LangNl, Text = "Vraag?" },
                    // English question translation missing
                },
                EndingAnswers = new List<EndingAnswer>(),
            },
        };

        var problems = _validator.Validate(project);

        Assert.Single(problems);
        Assert.Equal("QuestionTranslation", problems[0].EntityType);
        Assert.Equal(12, problems[0].EntityId);
        Assert.Equal("en", problems[0].LanguageCode);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Validate_ZeroCategories_ReturnsNoCategoryProblem()
    {
        var project = BilingualProject();
        project.Categories = new List<Category>();

        var problems = _validator.Validate(project);

        Assert.Single(problems);
        Assert.Equal("NoCategory", problems[0].EntityType);
        Assert.Null(problems[0].EntityId);
        Assert.Null(problems[0].LanguageCode);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Validate_MultipleProblems_ReturnsAll()
    {
        var project = BilingualProject();
        // Missing English project translation
        project.Translations = new List<ProjectTranslation>
        {
            new() { Language = LangNl, Name = "Naam", Topic = "Onderwerp" },
        };
        // No categories
        project.Categories = new List<Category>();

        var problems = _validator.Validate(project);

        Assert.Equal(2, problems.Count);
        Assert.Contains(problems, p => p.EntityType == "ProjectTranslation" && p.LanguageCode == "en");
        Assert.Contains(problems, p => p.EntityType == "NoCategory");
        await Task.CompletedTask;
    }
}
