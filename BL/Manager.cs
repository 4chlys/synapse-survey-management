using System.Linq.Expressions;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.DAL;

namespace SynapseEnqueteManagement.BL;

public class Manager : IManager
{
    private readonly IRepository _repository;

    public Manager(IRepository repository)
    {
        _repository = repository;
    }

    public IEnumerable<Question> GetQuestions()
    {
        return _repository.Read<Question>(
            includes: [q => q.Questionnaire]
        );
    }

    public Response AddResponse(string value, Question question, Participant participant)
    {
        var response = new Response { Value = value, Question = question, Participant = participant };
        return _repository.Create(response);
    }

    public Participant AddParticipant(Questionnaire questionnaire)
    {
        var participant = new Participant { Questionnaire = questionnaire };
        return _repository.Create(participant);
    }

    public Questionnaire GetFirstQuestionnaire()
    {
        return _repository.ReadSingle<Questionnaire>();
    }

    public IEnumerable<Response> GetAllResponses()
    {
        return _repository.Read<Response>();
    }

    public Questionnaire GetQuestionnaireByProjectId(int projectId)
    {
        return _repository.ReadSingle<Project>(
            filters: [p => p.ProjectId == projectId],
            includes: [p => p.Questionnaire],
            includeStrings: ["Questionnaire.Questions"]
        )?.Questionnaire;
    }

    public IEnumerable<Response> GetResponsesByProjectId(int projectId)
    {
        var questionnaire = GetQuestionnaireByProjectId(projectId);
        if (questionnaire is null)
            return Enumerable.Empty<Response>();

        var questionIds = questionnaire.Questions.Select(q => q.QuestionId).ToList();
        return _repository.Read<Response>(
            filters: [r => questionIds.Contains(r.Question.QuestionId)],
            includes: [r => r.Question, r => r.Participant]
        );
    }

    public Question GetQuestionById(int questionId)
    {
        return _repository.ReadById<Question>(questionId);
    }

    public Participant GetParticipantById(int participantId)
    {
        return _repository.ReadById<Participant>(participantId);
    }

    public Participant GetParticipantBySessionToken(Guid sessionToken)
    {
        return _repository.ReadSingle<Participant>(
            filters: [p => p.SessionToken == sessionToken]
        );
    }

    public bool HasParticipantResponded(Guid sessionToken)
    {
        var participant = _repository.ReadSingle<Participant>(
            filters: [p => p.SessionToken == sessionToken],
            includes: [p => p.Responses]
        );
        return participant?.Responses.Count > 0;
    }
}