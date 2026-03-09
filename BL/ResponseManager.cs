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

    public Response AddResponse(string value, Question question, Participant participant)
    {
        if (value?.Length > 500)
            throw new ArgumentException("Antwoord mag maximaal 500 tekens bevatten.", nameof(value));

        var response = new Response { Value = value, Question = question, Participant = participant };
        return _repository.Create(response);
    }

    public IEnumerable<Response> GetAllResponses()
    {
        return _repository.Read<Response>();
    }

    public IEnumerable<Response> GetResponsesByProjectId(int projectId)
    {
        var questionnaire = _repository.ReadSingle<Project>(
            filters: [p => p.ProjectId == projectId],
            includes: [p => p.Questionnaire],
            includeStrings: ["Questionnaire.Questions"]
        )?.Questionnaire;

        if (questionnaire is null)
            return Enumerable.Empty<Response>();

        var questionIds = questionnaire.Questions.Select(q => q.QuestionId).ToList();
        return _repository.Read<Response>(
            filters: [r => questionIds.Contains(r.Question.QuestionId)],
            includes: [r => r.Question, r => r.Participant]
        );
    }
}
