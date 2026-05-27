using SynapseEnqueteManagement.UI.Admin.Models.Auth;
using SynapseEnqueteManagement.UI.Admin.Models.Category;
using SynapseEnqueteManagement.UI.Admin.Models.Platform;
using SynapseEnqueteManagement.UI.Admin.Models.Project;
using SynapseEnqueteManagement.UI.Admin.Models.Question;
using QuestionMediaType = SynapseEnqueteManagement.UI.Admin.Models.Enums.QuestionMediaType;
using QuestionFormat = SynapseEnqueteManagement.UI.Admin.Models.Enums.QuestionFormat;

namespace SynapseEnqueteManagement.UI.Admin.Tests.ModelBinding;

// These tests answer one question the C# compiler cannot: ASP.NET Core's
// reflection-based form binder really does populate `{ get; init; }` properties
// on the ViewModel records. If a future framework change broke that, every
// form POST in UI-Admin would silently arrive empty — these tests are the
// canary for that scenario.
public class RecordViewModelBindingTests
{
    [Fact]
    public async Task LoginViewModel_BoundFromForm_PopulatesAllInitProperties()
    {
        var form = new Dictionary<string, StringValues>
        {
            ["Email"] = "admin@example.com",
            ["Password"] = "Sup3rSecret!",
            ["ReturnUrl"] = "/Project"
        };

        var (model, modelState) = await FormBinderTestHarness.BindAsync<LoginViewModel>(form);

        Assert.True(modelState.IsValid);
        Assert.Equal("admin@example.com", model.Email);
        Assert.Equal("Sup3rSecret!", model.Password);
        Assert.Equal("/Project", model.ReturnUrl);
    }

    [Fact]
    public async Task ChangePasswordViewModel_BoundFromForm_PopulatesAllInitProperties()
    {
        var form = new Dictionary<string, StringValues>
        {
            ["CurrentPassword"] = "OldPass1",
            ["NewPassword"] = "NewPass1",
            ["ConfirmNewPassword"] = "NewPass1"
        };

        var (model, modelState) = await FormBinderTestHarness.BindAsync<ChangePasswordViewModel>(form);

        Assert.True(modelState.IsValid);
        Assert.Equal("OldPass1", model.CurrentPassword);
        Assert.Equal("NewPass1", model.NewPassword);
        Assert.Equal("NewPass1", model.ConfirmNewPassword);
    }

    [Fact]
    public async Task CategoryFormViewModel_BoundFromForm_PopulatesAllInitProperties()
    {
        var form = new Dictionary<string, StringValues>
        {
            ["CategoryId"] = "42",
            ["Translations[0].LanguageCode"] = "nl",
            ["Translations[0].Name"] = "Climate",
            ["Translations[0].Description"] = "Ideas about the climate"
        };

        var (model, modelState) = await FormBinderTestHarness.BindAsync<CategoryFormViewModel>(form);

        Assert.True(modelState.IsValid);
        Assert.Equal(42, model.CategoryId);
        Assert.Single(model.Translations);
        Assert.Equal("nl", model.Translations.First().LanguageCode);
        Assert.Equal("Climate", model.Translations.First().Name);
        Assert.Equal("Ideas about the climate", model.Translations.First().Description);
    }

    [Fact]
    public async Task PlatformFormViewModel_BoundFromForm_PopulatesAllInitProperties()
    {
        var form = new Dictionary<string, StringValues>
        {
            ["ProjectPlatformId"] = "3",
            ["Name"] = "Acme",
            ["LogoUrl"] = "https://acme.example/logo.png",
            ["WebsiteUrl"] = "https://acme.example"
        };

        var (model, modelState) = await FormBinderTestHarness.BindAsync<PlatformFormViewModel>(form);

        Assert.True(modelState.IsValid);
        Assert.Equal(3, model.ProjectPlatformId);
        Assert.Equal("Acme", model.Name);
        Assert.Equal("https://acme.example/logo.png", model.LogoUrl);
        Assert.Equal("https://acme.example", model.WebsiteUrl);
    }

    [Fact]
    public async Task AddAdminViewModel_BoundFromForm_PopulatesAllInitProperties()
    {
        var form = new Dictionary<string, StringValues>
        {
            ["ProjectPlatformId"] = "5",
            ["PlatformName"] = "Acme",
            ["Email"] = "new.admin@example.com"
        };

        var (model, modelState) = await FormBinderTestHarness.BindAsync<AddAdminViewModel>(form);

        Assert.True(modelState.IsValid);
        Assert.Equal(5, model.ProjectPlatformId);
        Assert.Equal("Acme", model.PlatformName);
        Assert.Equal("new.admin@example.com", model.Email);
    }

    [Fact]
    public async Task PlatformConfigurationViewModel_BoundFromForm_PopulatesAllInitProperties()
    {
        var form = new Dictionary<string, StringValues>
        {
            ["ProjectPlatformId"] = "5",
            ["ContactName"] = "Alice",
            ["ContactEmail"] = "alice@acme.example",
            ["BillingReference"] = "PO-2024-001",
            ["AiAssistanceEnabled"] = "true",
            ["IdeationEnabled"] = "true",
            ["MaxProjects"] = "10",
            ["MaxAdmins"] = "4"
        };

        var (model, modelState) = await FormBinderTestHarness.BindAsync<PlatformConfigurationViewModel>(form);

        Assert.True(modelState.IsValid);
        Assert.Equal(5, model.ProjectPlatformId);
        Assert.Equal("Alice", model.ContactName);
        Assert.Equal("alice@acme.example", model.ContactEmail);
        Assert.Equal("PO-2024-001", model.BillingReference);
        Assert.True(model.AiAssistanceEnabled);
        Assert.True(model.IdeationEnabled);
        Assert.Equal(10, model.MaxProjects);
        Assert.Equal(4, model.MaxAdmins);
    }

    [Fact]
    public async Task CreateQuestionViewModel_BoundFromForm_PopulatesAllInitProperties()
    {
        // Per-language Text + MediaText + AnswerChoicesRaw bind through the
        // indexed `Translations[i].*` keys; structural fields stay flat.
        var form = new Dictionary<string, StringValues>
        {
            ["QuestionFormat"] = nameof(QuestionFormat.SingleChoice),
            ["IsMandatory"] = "true",
            ["SectionId"] = "2",
            ["MediaUrl"] = "https://media.example/poll.png",
            ["MediaType"] = nameof(QuestionMediaType.Image),
            ["MediaIsEmbed"] = "false",
            ["Translations[0].LanguageCode"]     = "nl",
            ["Translations[0].Text"]             = "Wat is je favoriete kleur?",
            ["Translations[0].AnswerChoicesRaw"] = "Rood\nGroen\nBlauw",
            ["Translations[0].MediaText"]        = "Kies één kleur uit de lijst",
            ["Translations[1].LanguageCode"]     = "en",
            ["Translations[1].Text"]             = "What is your favourite colour?",
            ["Translations[1].AnswerChoicesRaw"] = "Red\nGreen\nBlue",
            ["Translations[1].MediaText"]        = "Pick one colour from the list",
        };

        var (model, modelState) = await FormBinderTestHarness.BindAsync<CreateQuestionViewModel>(form);

        Assert.True(modelState.IsValid);
        Assert.Equal(QuestionFormat.SingleChoice, model.QuestionFormat);
        Assert.True(model.IsMandatory);
        Assert.Equal(2, model.SectionId);
        Assert.Equal("https://media.example/poll.png", model.MediaUrl);
        Assert.Equal(QuestionMediaType.Image, model.MediaType);
        Assert.False(model.MediaIsEmbed);
        Assert.Equal(2, model.Translations.Count);
        var nl = model.Translations.Single(t => t.LanguageCode == "nl");
        Assert.Equal("Wat is je favoriete kleur?", nl.Text);
        Assert.Equal("Rood\nGroen\nBlauw", nl.AnswerChoicesRaw);
        Assert.Equal("Kies één kleur uit de lijst", nl.MediaText);
        var en = model.Translations.Single(t => t.LanguageCode == "en");
        Assert.Equal("What is your favourite colour?", en.Text);
        Assert.Equal("Red\nGreen\nBlue", en.AnswerChoicesRaw);
        Assert.Equal("Pick one colour from the list", en.MediaText);
    }

    [Fact]
    public async Task EditQuestionViewModel_BoundFromForm_PopulatesAllInitProperties()
    {
        // EditQuestionViewModel inherits from CreateQuestionViewModel and adds
        // QuestionId; binder must populate both the base translations and the id.
        var form = new Dictionary<string, StringValues>
        {
            ["QuestionId"] = "11",
            ["QuestionFormat"] = nameof(QuestionFormat.MultipleChoice),
            ["IsMandatory"] = "false",
            ["SectionId"] = "9",
            ["MediaUrl"] = "https://media.example/edit.mp4",
            ["MediaType"] = nameof(QuestionMediaType.Video),
            ["MediaIsEmbed"] = "true",
            ["Translations[0].LanguageCode"]     = "nl",
            ["Translations[0].Text"]             = "Aangepaste vraag",
            ["Translations[0].AnswerChoicesRaw"] = "A\nB\nC\nD",
            ["Translations[0].MediaText"]        = "Bijgewerkte beschrijving",
        };

        var (model, modelState) = await FormBinderTestHarness.BindAsync<EditQuestionViewModel>(form);

        Assert.True(modelState.IsValid);
        Assert.Equal(11, model.QuestionId);
        Assert.Equal(QuestionFormat.MultipleChoice, model.QuestionFormat);
        Assert.False(model.IsMandatory);
        Assert.Equal(9, model.SectionId);
        Assert.Equal("https://media.example/edit.mp4", model.MediaUrl);
        Assert.Equal(QuestionMediaType.Video, model.MediaType);
        Assert.True(model.MediaIsEmbed);
        Assert.Single(model.Translations);
        var nl = model.Translations[0];
        Assert.Equal("nl", nl.LanguageCode);
        Assert.Equal("Aangepaste vraag", nl.Text);
        Assert.Equal("A\nB\nC\nD", nl.AnswerChoicesRaw);
        Assert.Equal("Bijgewerkte beschrijving", nl.MediaText);
    }

    // CreateProjectViewModel is the most complex of the bunch: it carries an
    // IList<SectionInputViewModel> (Sections — each with per-language translations) and an
    // ICollection<string> (SupportedLanguageCodes). If either abstraction
    // stopped binding, this test would catch it.
    [Fact]
    public async Task CreateProjectViewModel_BoundFromForm_PopulatesAllInitPropertiesIncludingCollections()
    {
        var form = new Dictionary<string, StringValues>
        {
            ["ProjectId"] = "100",
            ["ShortForm"] = "sample",
            ["InteractionMode"] = "HorizontalScroll",
            ["MinAge"] = "13",
            ["MaxAge"] = "25",
            ["ImageUrl"] = "https://images.example/cover.png",
            ["Sections[0].Translations[0].LanguageCode"] = "nl",
            ["Sections[0].Translations[0].Name"] = "Intro",
            ["Sections[1].Translations[0].LanguageCode"] = "nl",
            ["Sections[1].Translations[0].Name"] = "Body",
            ["ContactOptIn"] = "true",
            ["AllowCrossLanguageIdeas"] = "true",
            ["DefaultLanguageCode"] = "nl",
            ["SupportedLanguageCodes[0]"] = "nl",
            ["SupportedLanguageCodes[1]"] = "en",
            ["Theme"] = "Default",
            ["PrimaryColor"] = "#112233",
            ["CustomCss"] = ".x { color: red; }"
        };

        var (model, modelState) = await FormBinderTestHarness.BindAsync<CreateProjectViewModel>(form);

        Assert.True(modelState.IsValid);
        Assert.Equal(100, model.ProjectId);
        Assert.Equal("sample", model.ShortForm);
        Assert.Equal("HorizontalScroll", model.InteractionMode);
        Assert.Equal(13, model.MinAge);
        Assert.Equal(25, model.MaxAge);
        Assert.Equal("https://images.example/cover.png", model.ImageUrl);
        Assert.Equal(2, model.Sections.Count);
        Assert.Equal("nl", model.Sections[0].Translations[0].LanguageCode);
        Assert.Equal("Intro", model.Sections[0].Translations[0].Name);
        Assert.Equal("nl", model.Sections[1].Translations[0].LanguageCode);
        Assert.Equal("Body", model.Sections[1].Translations[0].Name);
        Assert.True(model.ContactOptIn);
        Assert.True(model.AllowCrossLanguageIdeas);
        Assert.Equal("nl", model.DefaultLanguageCode);
        Assert.Equal(new[] { "nl", "en" }, model.SupportedLanguageCodes);
        Assert.Equal("Default", model.Theme);
        Assert.Equal("#112233", model.PrimaryColor);
        Assert.Equal(".x { color: red; }", model.CustomCss);
    }
}
