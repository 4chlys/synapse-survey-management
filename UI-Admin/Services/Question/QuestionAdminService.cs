using System.Net.Http.Json;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.UI.Admin.Models.Question;

namespace SynapseEnqueteManagement.UI.Admin.Services.Question;

public class QuestionAdminService(HttpClient httpClient)
{
    private class QuestionRequest
    {
        public string Text { get; set; }
        public string QuestionFormat { get; set; }
        public bool IsMandatory { get; set; }
        public ICollection<string> AnswerChoices { get; set; }
        public int? SectionId { get; set; }
        public int? ParentQuestionId { get; set; }
        public string ParentTriggerValue { get; set; }
        public ICollection<string> EndingAnswers { get; set; }
        public string MediaUrl { get; set; }
        public QuestionMediaType? MediaType { get; set; }
        public bool MediaIsEmbed { get; set; }
        public string MediaText { get; set; }
    }

    private class MoveRequest
    {
        public int? ParentQuestionId { get; set; }
        public string ParentTriggerValue { get; set; }
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
        int? parentQuestionId = null, string parentTriggerValue = null,
        ICollection<string> endingAnswers = null,
        string mediaUrl = null, QuestionMediaType? mediaType = null,
        bool mediaIsEmbed = false, string mediaText = null)
    {
        var payload = new QuestionRequest
        {
            Text = text,
            QuestionFormat = questionFormat,
            IsMandatory = isMandatory,
            AnswerChoices = answerChoices,
            SectionId = sectionId,
            ParentQuestionId = parentQuestionId,
            ParentTriggerValue = parentTriggerValue,
            EndingAnswers = endingAnswers,
            MediaUrl = mediaUrl,
            MediaType = mediaType,
            MediaIsEmbed = mediaIsEmbed,
            MediaText = mediaText
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
        var payload = new QuestionRequest
        {
            Text = text,
            QuestionFormat = questionFormat,
            IsMandatory = isMandatory,
            AnswerChoices = answerChoices,
            SectionId = sectionId,
            EndingAnswers = endingAnswers,
            MediaUrl = mediaUrl,
            MediaType = mediaType,
            MediaIsEmbed = mediaIsEmbed,
            MediaText = mediaText
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
        int? parentQuestionId, string parentTriggerValue, int position)
    {
        var payload = new MoveRequest
        {
            ParentQuestionId = parentQuestionId,
            ParentTriggerValue = parentTriggerValue,
            Position = position
        };
        var response = await httpClient.PutAsJsonAsync(
            $"api/projects/{projectId}/questionnaire/questions/{questionId}/location", payload);
        response.EnsureSuccessStatusCode();
    }
}
