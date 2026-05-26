using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.BL.Interfaces;

public record SectionTranslationInput(string LanguageCode, string Name, string Description);
public record QuestionTranslationInput(string LanguageCode, string Text, ICollection<string> AnswerChoices, string MediaText);
public record EndingAnswerTranslationInput(string LanguageCode, string Value);

public interface IQuestionnaireManager
{
    Task<Questionnaire> GetQuestionnaireByProjectIdWithQuestionsReadOnlyAsync(int projectId);
    Task<Questionnaire> GetQuestionnaireByProjectIdReadOnlyAsync(int projectId);
    Task<Questionnaire> GetQuestionnaireByQuestionIdWithQuestionsReadOnlyAsync(int questionId);
    Task<Questionnaire> GetQuestionnaireByProjectIdWithTranslationsReadOnlyAsync(int projectId);
    Task<Questionnaire> GetQuestionnaireByQuestionIdWithTranslationsReadOnlyAsync(int questionId);
    Task<Question> GetQuestionByIdReadOnlyAsync(int questionId);
    Task<Question> GetQuestionByIdWithTranslationsReadOnlyAsync(int questionId);
    Task<bool> QuestionBelongsToProjectAsync(int questionId, int projectId);

    Task<Question> AddQuestionAsync(
        int projectId, int answerChoiceCount, QuestionFormat format, bool isMandatory,
        IEnumerable<QuestionTranslationInput> translations,
        int? sectionId = null,
        int? parentQuestionId = null, int? choiceIndex = null,
        IEnumerable<IEnumerable<EndingAnswerTranslationInput>> endingAnswers = null,
        string mediaUrl = null, QuestionMediaType? mediaType = null, bool mediaIsEmbed = false,
        CancellationToken ct = default);

    Task<IEnumerable<Question>> GetQuestionsForProjectAsync(int projectId);

    Task<bool> UpdateQuestionAsync(
        int questionId, int answerChoiceCount, QuestionFormat format, bool isMandatory,
        IEnumerable<QuestionTranslationInput> translations,
        int? sectionId = null,
        IEnumerable<IEnumerable<EndingAnswerTranslationInput>> endingAnswers = null,
        string mediaUrl = null, QuestionMediaType? mediaType = null, bool mediaIsEmbed = false,
        CancellationToken ct = default);

    Task<bool> DeleteQuestionAsync(int questionId, CancellationToken ct = default);

    Task<Question> MoveQuestionAsync(int questionId, int? newParentQuestionId,
        int? newChoiceIndex, int newPosition, CancellationToken ct = default);

    Task<Question> GetFirstQuestionAsync(int projectId);
    Task<int> GetQuestionCountAsync(int projectId);
    Task ValidateMandatoryAnswersAsync(int projectId, IEnumerable<(int QuestionId, string Value)> answers, HashSet<int> visibleQuestionIds = null);

    Task<IEnumerable<Section>> GetSectionsReadOnlyAsync(int projectId);
    Task<Section> CreateSectionAsync(int projectId, IEnumerable<SectionTranslationInput> translations, CancellationToken ct = default);
    Task<bool> UpdateSectionAsync(int sectionId, IEnumerable<SectionTranslationInput> translations, CancellationToken ct = default);
    Task<bool> DeleteSectionAsync(int sectionId, CancellationToken ct = default);
}
