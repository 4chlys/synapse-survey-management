using SynapseEnqueteManagement.BL.Domain.Project.Entities;
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

        var response = new Response { Value = value, Question = question, Participant = participant };
        return await _repository.CreateAsync(response);
    }

    public async Task<IEnumerable<Response>> GetAllResponsesAsync()
    {
        return await _repository.ReadAsync<Response>();
    }

    public async Task<IEnumerable<Response>> GetResponsesByProjectIdAsync(int projectId)
    {
        var project = await _repository.ReadSingleAsync<Project>(
            filters: [p => p.ProjectId == projectId],
            includes: [p => p.Questionnaire],
            includeStrings: ["Questionnaire.Questions"]
        );

        if (project?.Questionnaire is null)
            return Enumerable.Empty<Response>();

        var questionIds = project.Questionnaire.Questions.Select(q => q.QuestionId).ToList();
        return await _repository.ReadAsync<Response>(
            filters: [r => questionIds.Contains(r.Question.QuestionId)],
            includes: [r => r.Question, r => r.Participant]
        );
    }
}
