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

    public Response SaveResponse(string value, Question question, Participant participant)
    {
        var response = new Response{Value = value, Question = question, Participant = participant};
        return _repository.Create(response);
    }

    public Participant CreateParticipant(Questionnaire questionnaire)
    {
        var participant = new Participant{Questionnaire = questionnaire};
        return _repository.Create(participant);
    }

    public Questionnaire GetFirstQuestionnaire()
    {
        return _repository.Read<Questionnaire>().FirstOrDefault();
    }

    public IEnumerable<Response> GetAllResponses()
    {
        return _repository.Read<Response>();
    }
}