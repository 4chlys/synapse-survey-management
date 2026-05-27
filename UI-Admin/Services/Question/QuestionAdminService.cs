using System.Net.Http.Json;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
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
        // Slot count — every translation's AnswerChoices list has exactly this many entries.
        public int AnswerChoiceCount { get; init; }
        public string QuestionFormat { get; init; }
        public bool IsMandatory { get; init; }
        public int? SectionId { get; init; }
        public int? ParentQuestionId { get; init; }
        public int? ChoiceIndex { get; init; }
        public ICollection<QuestionTranslationDto> Translations { get; init; }
        // Outer collection = slot (one per ending-answer slot, mirroring choice slots).
        // Inner collection = per-language values for that slot. Null = preserve existing.
        public ICollection<ICollection<EndingAnswerTranslationDto>> EndingAnswers { get; init; }
        public string MediaUrl { get; init; }
        public QuestionMediaType? MediaType { get; init; }
        public bool MediaIsEmbed { get; init; }
    }

    private record MoveRequest(int? ParentQuestionId, int? ChoiceIndex, int Position);

    // Returns the full multilingual question shape (Translations[], per-language
    // EndingAnswers, AnswerChoiceCount) for the admin authoring view. Calls the
    // dedicated admin GET endpoint; the participant-facing list GET flattens to
    // one language and is unsuitable for edit rehydration.
    public async Task<List<AdminQuestionViewModel>> GetQuestionsAsync(int projectId)
    {
        var response = await
            httpClient.GetAsync($"api/projects/{projectId}/questionnaire/questions/admin");
        if (!response.IsSuccessStatusCode)
            return new List<AdminQuestionViewModel>();

        return await
                   response.Content.ReadFromJsonAsync<List<AdminQuestionViewModel>>()
               ?? new List<AdminQuestionViewModel>();
    }

    public async Task CreateQuestionAsync(int projectId, string questionFormat,
        bool isMandatory, int answerChoiceCount,
        ICollection<QuestionTranslationFormViewModel> translations,
        int? sectionId = null,
        int? parentQuestionId = null, int? choiceIndex = null,
        ICollection<EndingAnswerSlotFormViewModel> endingAnswerSlots = null,
        string mediaUrl = null, QuestionMediaType? mediaType = null,
        bool mediaIsEmbed = false)
    {
        var payload = new QuestionRequest
        {
            AnswerChoiceCount = answerChoiceCount,
            QuestionFormat = questionFormat,
            IsMandatory = isMandatory,
            SectionId = sectionId,
            ParentQuestionId = parentQuestionId,
            ChoiceIndex = choiceIndex,
            Translations = MapTranslations(translations),
            EndingAnswers = MapEndingAnswers(endingAnswerSlots),
            MediaUrl = mediaUrl,
            MediaType = mediaType,
            MediaIsEmbed = mediaIsEmbed,
        };
        var response = await
            httpClient.PostAsJsonAsync($"api/projects/{projectId}/questionnaire/questions",
                payload);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpdateQuestionAsync(int projectId, int questionId, string questionFormat,
        bool isMandatory, int answerChoiceCount,
        ICollection<QuestionTranslationFormViewModel> translations,
        int? sectionId = null,
        ICollection<EndingAnswerSlotFormViewModel> endingAnswerSlots = null,
        string mediaUrl = null, QuestionMediaType? mediaType = null,
        bool mediaIsEmbed = false)
    {
        var payload = new QuestionRequest
        {
            AnswerChoiceCount = answerChoiceCount,
            QuestionFormat = questionFormat,
            IsMandatory = isMandatory,
            SectionId = sectionId,
            Translations = MapTranslations(translations),
            EndingAnswers = MapEndingAnswers(endingAnswerSlots),
            MediaUrl = mediaUrl,
            MediaType = mediaType,
            MediaIsEmbed = mediaIsEmbed,
        };
        var response = await httpClient.PutAsJsonAsync(
            $"api/projects/{projectId}/questionnaire/questions/{questionId}", payload);
        response.EnsureSuccessStatusCode();
    }

    private static ICollection<QuestionTranslationDto> MapTranslations(
        ICollection<QuestionTranslationFormViewModel> translations) =>
        (translations ?? new List<QuestionTranslationFormViewModel>())
            .Select(t => new QuestionTranslationDto
            {
                LanguageCode = t.LanguageCode,
                Text = t.Text,
                AnswerChoices = t.GetAnswerChoices(),
                MediaText = t.MediaText
            })
            .ToList();

    // null → null on the wire (manager preserves existing endings on update).
    // Empty list → [] on the wire (manager clears endings). Non-empty → mapped.
    // This distinction matters for the tree's "Ends survey" toggle path, which
    // needs to explicitly clear all endings when every toggle is unchecked.
    private static ICollection<ICollection<EndingAnswerTranslationDto>> MapEndingAnswers(
        ICollection<EndingAnswerSlotFormViewModel> endingAnswerSlots)
    {
        if (endingAnswerSlots is null) return null;
        return endingAnswerSlots
            .Select(slot => (ICollection<EndingAnswerTranslationDto>)slot.Translations
                .Select(t => new EndingAnswerTranslationDto
                {
                    LanguageCode = t.LanguageCode,
                    Value = t.Value ?? string.Empty
                })
                .ToList())
            .ToList();
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
