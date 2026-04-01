using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.DAL;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.BL.Implementation;

public class ResponseManager(IRepository repository, IResponseValidatorFactory validatorFactory) : IResponseManager
{
    public async Task<Response> AddResponseAsync(string value, Question question, Participant participant)
    {
        if (value?.Length > 500)
            throw new ArgumentException("Response must not exceed 500 characters.", nameof(value));

        var validator = validatorFactory.GetValidator(question.QuestionFormat);
        validator.Validate(value, question);

        var response = new Response { Value = value, Question = question, Participant = participant, CreatedAt = DateTime.UtcNow };
        return await repository.CreateAsync(response);
    }

    public async Task<Response> UpdateOrCreateResponseAsync(string value, Question question, Participant participant)
    {
        if (value?.Length > 500)
            throw new ArgumentException("Response must not exceed 500 characters.", nameof(value));

        var validator = validatorFactory.GetValidator(question.QuestionFormat);
        validator.Validate(value, question);

        var existing = await repository.ReadSingleAsync<Response>(
            filters:
            [
                r => r.Question.QuestionId == question.QuestionId,
                r => r.Participant.ParticipantId == participant.ParticipantId
            ],
            tracked: true);

        if (existing is not null)
        {
            existing.Value = value;
            existing.IsCommitted = false;
            await repository.UpdateAsync(existing);
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
        return await repository.CreateAsync(response);
    }

    public async Task CommitResponsesAsync(int participantId, HashSet<int> visibleQuestionIds)
    {
        var responses = await repository.ReadAsync<Response>(
            filters: [r => r.Participant.ParticipantId == participantId],
            includeStrings: ["Question"],
            tracked: true);

        foreach (var response in responses)
        {
            var shouldCommit = visibleQuestionIds.Contains(response.Question.QuestionId);
            if (response.IsCommitted != shouldCommit)
            {
                response.IsCommitted = shouldCommit;
                await repository.UpdateAsync(response);
            }
        }
    }

    public async Task<IEnumerable<Response>> GetResponsesForParticipantAsync(int participantId)
    {
        return await repository.ReadAsync<Response>(
            filters: [r => r.Participant.ParticipantId == participantId],
            includeStrings: ["Question"]);
    }
}