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
        if (value?.Length > 500)
            throw new ArgumentException("Response must not exceed 500 characters.", nameof(value));

        var question = await questionRepository.ReadByIdForUpdateAsync(questionId)
                       ?? throw new KeyNotFoundException($"Question {questionId} not found.");

        var validator = validatorFactory.GetValidator(question.QuestionFormat);
        validator.Validate(value, question);

        var response = new Response { Value = value, Question = question, Participant = participant, CreatedAt = DateTime.UtcNow };
        var created = await responseRepository.CreateAsync(response);
        await uow.SaveChangesAsync(ct);
        return created;
    }

    public async Task<Response> UpdateOrCreateResponseAsync(int questionId, string value, Participant participant, CancellationToken ct = default)
    {
        if (value?.Length > 500)
            throw new ArgumentException("Response must not exceed 500 characters.", nameof(value));

        var question = await questionRepository.ReadByIdForUpdateAsync(questionId)
                       ?? throw new KeyNotFoundException($"Question {questionId} not found.");

        var validator = validatorFactory.GetValidator(question.QuestionFormat);
        validator.Validate(value, question);

        var existing = await responseRepository.ReadByQuestionAndParticipantForUpdateAsync(questionId, participant.ParticipantId);

        if (existing is not null)
        {
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
            CreatedAt = DateTime.UtcNow,
            IsCommitted = false
        };
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

    public Task<Response> GetResponseByQuestionIdAndParticipantIdReadOnlyAsync(int questionId, int participantId)
        => responseRepository.ReadByQuestionAndParticipantReadOnlyAsync(questionId, participantId);

    public Task<IEnumerable<Response>> GetResponsesForParticipantReadOnlyAsync(int participantId)
        => responseRepository.ReadByParticipantWithQuestionReadOnlyAsync(participantId);
}
