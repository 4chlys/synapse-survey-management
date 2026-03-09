using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.DAL;

namespace SynapseEnqueteManagement.BL;

public class QuestionnaireManager : IQuestionnaireManager
{
    private readonly IRepository _repository;

    public QuestionnaireManager(IRepository repository)
    {
        _repository = repository;
    }

    public IEnumerable<Question> GetQuestions()
    {
        return _repository.Read<Question>(
            includes: [q => q.Questionnaire]
        );
    }

    public Questionnaire GetFirstQuestionnaire()
    {
        return _repository.ReadSingle<Questionnaire>();
    }

    public Questionnaire GetQuestionnaireByProjectId(int projectId)
    {
        return _repository.ReadSingle<Project>(
            filters: [p => p.ProjectId == projectId],
            includes: [p => p.Questionnaire],
            includeStrings: ["Questionnaire.Questions"]
        )?.Questionnaire;
    }

    public Question GetQuestionById(int questionId)
    {
        return _repository.ReadById<Question>(questionId);
    }
}
