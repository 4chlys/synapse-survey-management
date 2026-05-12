using System.Net.Http.Json;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.UI.Admin.Models.Question;

namespace SynapseEnqueteManagement.UI.Admin.Services.Question;

public class QuestionAdminService(HttpClient httpClient)
{
    private class QuestionTranslationDto
    {
        public string LanguageCode { get; set; }
        public string Text { get; set; }
        public ICollection<string> AnswerChoices { get; set; }
        public string MediaText { get; set; }
    }

    private class EndingAnswerTranslationDto
    {
        public string LanguageCode { get; set; }
        public string Value { get; set; }
    }

    private class QuestionRequest
    {
        public int AnswerChoiceCount { get; set; }
        public string QuestionFormat { get; set; }
        public bool IsMandatory { get; set; }
        public int? SectionId { get; set; }
        public int? ParentQuestionId { get; set; }
        public int? ChoiceIndex { get; set; }
        public ICollection<QuestionTranslationDto> Translations { get; set; }
        public ICollection<ICollection<EndingAnswerTranslationDto>> EndingAnswers { get; set; }
        public string MediaUrl { get; set; }
        public QuestionMediaType? MediaType { get; set; }
        public bool MediaIsEmbed { get; set; }
    }

    private class MoveRequest
    {
        public int? ParentQuestionId { get; set; }
        public int? ChoiceIndex { get; set; }
        public int Position { get; set; }
    }

    public async Task<List<QuestionViewModel>> GetQuestionsAsync(int projectId)
    {
        var response = await
            httpClient.GetAsync($"api/projects/{projectId}/questionnaire/questions");
        if (!response.IsSuccessStatusCode)
            return new List<QuestionViewModel>();

        return await
                   response.Content.ReadFromJsonAsync<List<QuestionViewModel>>()
               ?? new List<QuestionViewModel>();
    }

    public async Task CreateQuestionAsync(int projectId, string text, string questionFormat,
        bool isMandatory, ICollection<string> answerChoices, int? sectionId = null,
        int? parentQuestionId = null, int? choiceIndex = null,
        ICollection<string> endingAnswers = null,
        string mediaUrl = null, QuestionMediaType? mediaType = null,
        bool mediaIsEmbed = false, string mediaText = null)
    {
        var translation = new QuestionTranslationDto
        {
            LanguageCode = "nl",
            Text = text,
            AnswerChoices = answerChoices ?? new List<string>(),
            MediaText = mediaText
        };
        var payload = new QuestionRequest
        {
            AnswerChoiceCount = answerChoices?.Count ?? 0,
            QuestionFormat = questionFormat,
            IsMandatory = isMandatory,
            SectionId = sectionId,
            ParentQuestionId = parentQuestionId,
            ChoiceIndex = choiceIndex,
            Translations = new List<QuestionTranslationDto> { translation },
            EndingAnswers = endingAnswers?
                .Select(v => (ICollection<EndingAnswerTranslationDto>)new List<EndingAnswerTranslationDto>
                {
                    new() { LanguageCode = "nl", Value = v }
                }).ToList() ?? new List<ICollection<EndingAnswerTranslationDto>>(),
            MediaUrl = mediaUrl,
            MediaType = mediaType,
            MediaIsEmbed = mediaIsEmbed,
        };
        var response = await
            httpClient.PostAsJsonAsync($"api/projects/{projectId}/questionnaire/questions",
                payload);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpdateQuestionAsync(int projectId, int questionId, string text, string questionFormat,
        bool isMandatory, ICollection<string> answerChoices, int? sectionId = null,
        ICollection<string> endingAnswers = null,
        string mediaUrl = null, QuestionMediaType? mediaType = null,
        bool mediaIsEmbed = false, string mediaText = null)
    {
        var translation = new QuestionTranslationDto
        {
            LanguageCode = "nl",
            Text = text,
            AnswerChoices = answerChoices ?? new List<string>(),
            MediaText = mediaText
        };
        var payload = new QuestionRequest
        {
            AnswerChoiceCount = answerChoices?.Count ?? 0,
            QuestionFormat = questionFormat,
            IsMandatory = isMandatory,
            SectionId = sectionId,
            Translations = new List<QuestionTranslationDto> { translation },
            EndingAnswers = endingAnswers?
                .Select(v => (ICollection<EndingAnswerTranslationDto>)new List<EndingAnswerTranslationDto>
                {
                    new() { LanguageCode = "nl", Value = v }
                }).ToList() ?? new List<ICollection<EndingAnswerTranslationDto>>(),
            MediaUrl = mediaUrl,
            MediaType = mediaType,
            MediaIsEmbed = mediaIsEmbed,
        };
        var response = await httpClient.PutAsJsonAsync(
            $"api/projects/{projectId}/questionnaire/questions/{questionId}", payload);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteQuestionAsync(int projectId, int questionId)
    {
        var response = await httpClient.DeleteAsync(
            $"api/projects/{projectId}/questionnaire/questions/{questionId}");
        response.EnsureSuccessStatusCode();
    }

    public async Task MoveQuestionAsync(int projectId, int questionId,
        int? parentQuestionId, int? choiceIndex, int position)
    {
        var payload = new MoveRequest
        {
            ParentQuestionId = parentQuestionId,
            ChoiceIndex = choiceIndex,
            Position = position
        };
        var response = await httpClient.PutAsJsonAsync(
            $"api/projects/{projectId}/questionnaire/questions/{questionId}/location", payload);
        response.EnsureSuccessStatusCode();
    }
}
