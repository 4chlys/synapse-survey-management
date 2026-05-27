using System.Net.Http.Json;
using SynapseEnqueteManagement.UI.Admin.Models.Enums;
using SynapseEnqueteManagement.UI.Admin.Models.Question;

namespace SynapseEnqueteManagement.UI.Admin.Services.Question;

public class QuestionAdminService(HttpClient httpClient)
{
    private record QuestionTranslationDto
    {
        public string LanguageCode { get; init; }
        public string Text { get; init; }
        public ICollection<string> AnswerChoices { get; init; }
        public string MediaText { get; init; }
    }

    private record EndingAnswerTranslationDto
    {
        public string LanguageCode { get; init; }
        public string Value { get; init; }
    }

    private record QuestionRequest
    {
        public int AnswerChoiceCount { get; init; }
        public string QuestionFormat { get; init; }
        public bool IsMandatory { get; init; }
        public int? SectionId { get; init; }
        public int? ParentQuestionId { get; init; }
        public int? ChoiceIndex { get; init; }
        public ICollection<QuestionTranslationDto> Translations { get; init; }
        public ICollection<ICollection<EndingAnswerTranslationDto>> EndingAnswers { get; init; }
        public string MediaUrl { get; init; }
        public QuestionMediaType? MediaType { get; init; }
        public bool MediaIsEmbed { get; init; }
    }

    private record MoveRequest(int? ParentQuestionId, int? ChoiceIndex, int Position);

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
        var payload = new MoveRequest(parentQuestionId, choiceIndex, position);
        var response = await httpClient.PutAsJsonAsync(
            $"api/projects/{projectId}/questionnaire/questions/{questionId}/location", payload);
        response.EnsureSuccessStatusCode();
    }
}
