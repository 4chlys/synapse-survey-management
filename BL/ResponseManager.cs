using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.DAL;

namespace SynapseEnqueteManagement.BL;

public class ResponseManager(IRepository repository) : IResponseManager
{
    public async Task<Response> AddResponseAsync(string value, Question question, Participant participant)
    {
        if (value?.Length > 500)
            throw new ArgumentException("Antwoord mag maximaal 500 tekens bevatten.", nameof(value));

        if (question.QuestionFormat == QuestionFormat.SingleChoice
            && !question.AnswerChoices.Contains(value))
            throw new ArgumentException("Ongeldig antwoord: keuze staat niet in de lijst.", nameof(value));

        var response = new Response { Value = value, Question = question, Participant = participant };
        return await repository.CreateAsync(response);
    }

    public async Task<IEnumerable<Response>> GetAllResponsesAsync()
    {
        return await repository.ReadAsync<Response>();
    }

    public async Task<IEnumerable<Response>> GetResponsesByProjectIdAsync(int projectId)
    {
        var project = await repository.ReadSingleAsync<Project>(
            filters: [p => p.ProjectId == projectId],
            includes: [p => p.Questionnaire],
            includeStrings: ["Questionnaire.Questions"]
        );

        if (project?.Questionnaire is null)
            return Enumerable.Empty<Response>();

        var questionIds = project.Questionnaire.Questions.Select(q => q.QuestionId).ToList();
        return await repository.ReadAsync<Response>(
            filters: [r => questionIds.Contains(r.Question.QuestionId)],
            includes: [r => r.Question, r => r.Participant]
        );
    }
}
