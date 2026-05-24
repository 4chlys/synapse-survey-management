using SynapseEnqueteManagement.UI.Admin.Models.Auth;
using SynapseEnqueteManagement.UI.Admin.Models.Category;
using SynapseEnqueteManagement.UI.Admin.Models.Platform;
using SynapseEnqueteManagement.UI.Admin.Models.Project;
using SynapseEnqueteManagement.UI.Admin.Models.Question;
using DomainMediaType = SynapseEnqueteManagement.BL.Domain.Project.Enums.QuestionMediaType;
using BlQuestionFormat = SynapseEnqueteManagement.BL.Domain.Project.Enums.QuestionFormat;
using AdminQuestionFormat = SynapseEnqueteManagement.UI.Admin.Models.Enums.QuestionFormat;

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
            ["Name"] = "Climate",
            ["Description"] = "Ideas about the climate"
        };

        var (model, modelState) = await FormBinderTestHarness.BindAsync<CategoryFormViewModel>(form);

        Assert.True(modelState.IsValid);
        Assert.Equal(42, model.CategoryId);
        Assert.Equal("Climate", model.Name);
        Assert.Equal("Ideas about the climate", model.Description);
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
            ["MaxAdmins"] = "4",
            ["AiModelName"] = "gemini-2.5-flash",
            ["CostDashboardUrl"] = "https://dashboards.example/acme"
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
        Assert.Equal("gemini-2.5-flash", model.AiModelName);
        Assert.Equal("https://dashboards.example/acme", model.CostDashboardUrl);
    }

    [Fact]
    public async Task CreateQuestionViewModel_BoundFromForm_PopulatesAllInitProperties()
    {
        var form = new Dictionary<string, StringValues>
        {
            ["Text"] = "What is your favourite colour?",
            ["QuestionFormat"] = nameof(BlQuestionFormat.SingleChoice),
            ["IsMandatory"] = "true",
            ["AnswerChoicesRaw"] = "Red\nGreen\nBlue",
            ["SectionId"] = "2",
            ["MediaUrl"] = "https://media.example/poll.png",
            ["MediaType"] = nameof(DomainMediaType.Image),
            ["MediaIsEmbed"] = "false",
            ["MediaText"] = "Pick one colour from the list"
        };

        var (model, modelState) = await FormBinderTestHarness.BindAsync<CreateQuestionViewModel>(form);

        Assert.True(modelState.IsValid);
        Assert.Equal("What is your favourite colour?", model.Text);
        Assert.Equal(BlQuestionFormat.SingleChoice, model.QuestionFormat);
        Assert.True(model.IsMandatory);
        Assert.Equal("Red\nGreen\nBlue", model.AnswerChoicesRaw);
        Assert.Equal(2, model.SectionId);
        Assert.Equal("https://media.example/poll.png", model.MediaUrl);
        Assert.Equal(DomainMediaType.Image, model.MediaType);
        Assert.False(model.MediaIsEmbed);
        Assert.Equal("Pick one colour from the list", model.MediaText);
    }

    [Fact]
    public async Task EditQuestionViewModel_BoundFromForm_PopulatesAllInitProperties()
    {
        var form = new Dictionary<string, StringValues>
        {
            ["QuestionId"] = "11",
            ["Text"] = "Refined question text",
            ["QuestionFormat"] = nameof(AdminQuestionFormat.MultipleChoice),
            ["IsMandatory"] = "false",
            ["AnswerChoicesRaw"] = "A\nB\nC\nD",
            ["SectionId"] = "9",
            ["MediaUrl"] = "https://media.example/edit.mp4",
            ["MediaType"] = nameof(DomainMediaType.Video),
            ["MediaIsEmbed"] = "true",
            ["MediaText"] = "Updated description"
        };

        var (model, modelState) = await FormBinderTestHarness.BindAsync<EditQuestionViewModel>(form);

        Assert.True(modelState.IsValid);
        Assert.Equal(11, model.QuestionId);
        Assert.Equal("Refined question text", model.Text);
        Assert.Equal(AdminQuestionFormat.MultipleChoice, model.QuestionFormat);
        Assert.False(model.IsMandatory);
        Assert.Equal("A\nB\nC\nD", model.AnswerChoicesRaw);
        Assert.Equal(9, model.SectionId);
        Assert.Equal("https://media.example/edit.mp4", model.MediaUrl);
        Assert.Equal(DomainMediaType.Video, model.MediaType);
        Assert.True(model.MediaIsEmbed);
        Assert.Equal("Updated description", model.MediaText);
    }

    // CreateProjectViewModel is the most complex of the bunch: it carries an
    // IList<string> (Sections — kept as IList so views can index it) and an
    // ICollection<string> (SupportedLanguageCodes). If either abstraction
    // stopped binding, this test would catch it.
    [Fact]
    public async Task CreateProjectViewModel_BoundFromForm_PopulatesAllInitPropertiesIncludingCollections()
    {
        var form = new Dictionary<string, StringValues>
        {
            ["ProjectId"] = "100",
            ["Name"] = "Project Sample",
            ["ShortForm"] = "sample",
            ["Topic"] = "Civic ideas",
            ["Introduction"] = "Welcome to the project",
            ["InteractionMode"] = "HorizontalScroll",
            ["AiAssistance"] = "AskFirst",
            ["MinAge"] = "13",
            ["MaxAge"] = "25",
            ["ImageUrl"] = "https://images.example/cover.png",
            ["Sections[0]"] = "Intro",
            ["Sections[1]"] = "Body",
            ["Sections[2]"] = "Wrap-up",
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
        Assert.Equal("Project Sample", model.Name);
        Assert.Equal("sample", model.ShortForm);
        Assert.Equal("Civic ideas", model.Topic);
        Assert.Equal("Welcome to the project", model.Introduction);
        Assert.Equal("HorizontalScroll", model.InteractionMode);
        Assert.Equal("AskFirst", model.AiAssistance);
        Assert.Equal(13, model.MinAge);
        Assert.Equal(25, model.MaxAge);
        Assert.Equal("https://images.example/cover.png", model.ImageUrl);
        Assert.Equal(new[] { "Intro", "Body", "Wrap-up" }, model.Sections);
        Assert.True(model.ContactOptIn);
        Assert.True(model.AllowCrossLanguageIdeas);
        Assert.Equal("nl", model.DefaultLanguageCode);
        Assert.Equal(new[] { "nl", "en" }, model.SupportedLanguageCodes);
        Assert.Equal("Default", model.Theme);
        Assert.Equal("#112233", model.PrimaryColor);
        Assert.Equal(".x { color: red; }", model.CustomCss);
    }
}
