using SynapseEnqueteManagement.BL.Domain.I18n.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.BL.Helpers;
using SynapseEnqueteManagement.DAL.Interfaces;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.BL.Implementation;

public class ResponseManager(
    IResponseRepository responseRepository,
    IQuestionRepository questionRepository,
    IUnitOfWork uow,
    IResponseValidatorFactory validatorFactory) : IResponseManager
{
    public async Task<Response> AddResponseAsync(int questionId, string value, Participant participant, CancellationToken ct = default)
    {
        var question = await questionRepository.ReadByIdWithTranslationsForUpdateAsync(questionId)
                       ?? throw new KeyNotFoundException($"Question {questionId} not found.");

        var language = ContentLanguageResolver.Resolve(participant, null, null);
        var validator = validatorFactory.GetValidator(question.QuestionFormat);
        validator.Validate(value, question, language?.Code);

        var response = new Response
        {
            Value = value,
            Question = question,
            Participant = participant,
            Language = language,
            CreatedAt = DateTime.UtcNow
        };
        EntityValidator.Validate(response);
        var created = await responseRepository.CreateAsync(response);
        await uow.SaveChangesAsync(ct);
        return created;
    }

    public async Task<Response> UpdateOrCreateResponseAsync(int questionId, string value, Participant participant, CancellationToken ct = default)
    {
        var question = await questionRepository.ReadByIdWithTranslationsForUpdateAsync(questionId)
                       ?? throw new KeyNotFoundException($"Question {questionId} not found.");

        var language = ContentLanguageResolver.Resolve(participant, null, null);
        var validator = validatorFactory.GetValidator(question.QuestionFormat);
        validator.Validate(value, question, language?.Code);

        var existing = await responseRepository.ReadByQuestionAndParticipantForUpdateAsync(questionId, participant.ParticipantId);

        if (existing is not null)
        {
            EntityValidator.ValidateProperty(existing, value, nameof(Response.Value));
            existing.Value = value;
            existing.IsCommitted = false;
            await uow.SaveChangesAsync(ct);
            return existing;
        }

        var response = new Response
        {
            Value = value,
            Question = question,
            Participant = participant,
            Language = language,
            CreatedAt = DateTime.UtcNow,
            IsCommitted = false
        };
        EntityValidator.Validate(response);
        var created = await responseRepository.CreateAsync(response);
        await uow.SaveChangesAsync(ct);
        return created;
    }

    public async Task CommitResponsesAsync(int participantId, HashSet<int> visibleQuestionIds, CancellationToken ct = default)
    {
        var responses = await responseRepository.ReadByParticipantWithQuestionForUpdateAsync(participantId);

        var changed = false;
        foreach (var response in responses)
        {
            var shouldCommit = visibleQuestionIds.Contains(response.Question.QuestionId);
            if (response.IsCommitted != shouldCommit)
            {
                response.IsCommitted = shouldCommit;
                changed = true;
            }
        }
        if (changed) await uow.SaveChangesAsync(ct);
    }

    public Task<IEnumerable<Response>> GetResponsesForParticipantReadOnlyAsync(int participantId)
        => responseRepository.ReadByParticipantWithQuestionReadOnlyAsync(participantId);

    public Task<IEnumerable<Response>> GetResponsesForParticipantReadOnlyAsync(Participant participant)
        => responseRepository.ReadByParticipantWithQuestionReadOnlyAsync(participant.ParticipantId);

    public Task RemoveAllAsync(Participant participant, CancellationToken ct = default)
        => uow.ExecuteInTransactionAsync(_ => responseRepository.DeleteAllByParticipantIdAsync(participant.ParticipantId), ct);

    public Task<IEnumerable<Response>> GetCommittedResponsesForProjectReadOnlyAsync(int projectId)
        => responseRepository.ReadCommittedByProjectWithQuestionReadOnlyAsync(projectId);

    public async Task RetranslateResponsesForLanguageAsync(int participantId, Language targetLanguage, CancellationToken ct = default)
    {
        if (targetLanguage is null) throw new ArgumentNullException(nameof(targetLanguage));

        var responses = (await responseRepository.ReadByParticipantWithQuestionTranslationsForUpdateAsync(participantId)).ToList();
        if (responses.Count == 0) return;

        var changed = false;
        foreach (var response in responses)
        {
            var sourceCode = response.Language?.Code;
            var newValue = TranslateValue(response.Value, response.Question, sourceCode, targetLanguage.Code);

            if (newValue != response.Value)
            {
                response.Value = newValue;
                changed = true;
            }
            if (response.Language?.Code != targetLanguage.Code)
            {
                response.Language = targetLanguage;
                changed = true;
            }
        }
        if (changed) await uow.SaveChangesAsync(ct);
    }

    private static string TranslateValue(string value, Question question, string sourceLanguageCode, string targetLanguageCode)
    {
        if (string.IsNullOrEmpty(value)) return value;
        // Open responses are free user text and Likert is a numeric index — both are language-independent.
        if (question.QuestionFormat == QuestionFormat.Open
            || question.QuestionFormat == QuestionFormat.Likert) return value;
        if (sourceLanguageCode == targetLanguageCode) return value;

        var sourceChoices = ChoicesFor(question, sourceLanguageCode);
        var targetChoices = ChoicesFor(question, targetLanguageCode);
        if (sourceChoices.Count == 0 || targetChoices.Count == 0) return value;

        if (question.QuestionFormat == QuestionFormat.SingleChoice)
        {
            var idx = sourceChoices.IndexOf(value);
            return idx >= 0 && idx < targetChoices.Count ? targetChoices[idx] : value;
        }

        // MultipleChoice — values are stored as ", "-joined choice texts (see SurveyFlowManager.SplitSelected)
        var parts = MultipleChoiceValueSerializer.Split(value);
        var translated = new List<string>(parts.Count);
        foreach (var part in parts)
        {
            var idx = sourceChoices.IndexOf(part);
            translated.Add(idx >= 0 && idx < targetChoices.Count ? targetChoices[idx] : part);
        }
        return MultipleChoiceValueSerializer.Join(translated);
    }

    private static List<string> ChoicesFor(Question question, string languageCode)
        => TranslationPicker.Pick(question.Translations, languageCode)?.AnswerChoices?.ToList()
           ?? new List<string>();
}
