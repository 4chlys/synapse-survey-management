using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.DAL;

namespace SynapseEnqueteManagement.BL;

public class ResponseManager : IResponseManager
{
    private readonly IRepository _repository;

    public ResponseManager(IRepository repository)
    {
        _repository = repository;
    }

    public async Task<Response> AddResponseAsync(string value, Question question, Participant participant)
    {
        if (value?.Length > 500)
            throw new ArgumentException("Antwoord mag maximaal 500 tekens bevatten.", nameof(value));

        if (question.QuestionFormat == QuestionFormat.SingleChoice
            && !question.AnswerChoices.Contains(value))
            throw new ArgumentException("Ongeldig antwoord: keuze staat niet in de lijst.", nameof(value));

        var response = new Response { Value = value, Question = question, Participant = participant };
        return await _repository.CreateAsync(response);
    }

    public async Task AddResponsesAsync(Participant participant, IEnumerable<(int QuestionId, string Value)> answers)
    {
        foreach (var answer in answers)
        {
            if (string.IsNullOrWhiteSpace(answer.Value)) continue;

            var question = await _repository.ReadByIdAsync<Question>(answer.QuestionId);
            if (question is null) continue;

            await AddResponseAsync(answer.Value, question, participant);
        }
    }

    public async Task<IEnumerable<Response>> GetResponsesByProjectIdAsync(int projectId)
    {
        return await _repository.ReadAsync<Response>(
            filters: [r => r.Question.Questionnaire.Project.ProjectId == projectId],
            includes: [r => r.Question, r => r.Participant]
        );
    }
}