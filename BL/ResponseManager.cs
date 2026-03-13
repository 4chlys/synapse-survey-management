using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.DAL;

namespace SynapseEnqueteManagement.BL;

public class ResponseManager(IRepository repository) : IResponseManager
{
    public async Task<Response> AddResponseAsync(string value, Question question, Participant participant)
    {
        if (value?.Length > 500)
            throw new ArgumentException("Response must not exceed 500 characters.", nameof(value));

        if (question.QuestionFormat == QuestionFormat.SingleChoice
            && !question.AnswerChoices.Contains(value))
            throw new ArgumentException("Invalid answer: choice is not in the list.", nameof(value));

        var response = new Response { Value = value, Question = question, Participant = participant };
        return await repository.CreateAsync(response);
    }

    public async Task AddResponsesAsync(Participant participant, IEnumerable<(int QuestionId, string Value)> answers)
    {
        foreach (var answer in answers)
        {
            if (string.IsNullOrWhiteSpace(answer.Value)) continue;

            var question = await repository.ReadByIdAsync<Question>(answer.QuestionId);
            if (question is null) continue;

            await AddResponseAsync(answer.Value, question, participant);
        }
    }

    public async Task<IEnumerable<Response>> GetResponsesByProjectIdAsync(int projectId)
    {
        return await repository.ReadAsync<Response>(
            filters: [r => r.Question.Questionnaire.Project.ProjectId == projectId],
            includes: [r => r.Question, r => r.Participant]
        );
    }
}