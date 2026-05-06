using SynapseEnqueteManagement.BL.Domain.Project.Entities;
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

        var validator = validatorFactory.GetValidator(question.QuestionFormat);
        validator.Validate(value, question);

        var response = new Response
        {
            Value = value,
            Question = question,
            Participant = participant,
            Language = participant.Language ?? participant.Project?.DefaultLanguage,
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

        var validator = validatorFactory.GetValidator(question.QuestionFormat);
        validator.Validate(value, question);

        var existing = await responseRepository.ReadByQuestionAndParticipantForUpdateAsync(questionId, participant.ParticipantId);

        if (existing is not null)
        {
            EntityValidator.ValidateProperty(existing, value, nameof(Response.Value));
            existing.Value = value;
            existing.IsCommitted = false;
            responseRepository.Update(existing);
            await uow.SaveChangesAsync(ct);
            return existing;
        }

        var response = new Response
        {
            Value = value,
            Question = question,
            Participant = participant,
            Language = participant.Language ?? participant.Project?.DefaultLanguage,
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
                responseRepository.Update(response);
                changed = true;
            }
        }
        if (changed) await uow.SaveChangesAsync(ct);
    }

    public Task<IEnumerable<Response>> GetResponsesForParticipantReadOnlyAsync(int participantId)
        => responseRepository.ReadByParticipantWithQuestionReadOnlyAsync(participantId);

    public Task<IEnumerable<Response>> GetResponsesForParticipantReadOnlyAsync(Participant participant)
        => responseRepository.ReadByParticipantWithQuestionReadOnlyAsync(participant.ParticipantId);

    public async Task RemoveAllAsync(Participant participant, CancellationToken ct = default)
    {
        await responseRepository.DeleteAllByParticipantIdAsync(participant.ParticipantId);
        await uow.SaveChangesAsync(ct);
    }
}
