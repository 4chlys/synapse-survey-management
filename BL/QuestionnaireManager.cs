using System.ComponentModel.DataAnnotations;
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

    public void ValidateMandatoryAnswers(Questionnaire questionnaire, IEnumerable<(int QuestionId, string Value)> answers)
    {
        var answeredIds = answers
            .Where(a => !string.IsNullOrWhiteSpace(a.Value))
            .Select(a => a.QuestionId)
            .ToHashSet();

        var missing = questionnaire.Questions
            .Where(q => q.IsMandatory && !answeredIds.Contains(q.QuestionId))
            .Select(q => q.QuestionId)
            .ToList();

        if (missing.Count > 0)
            throw new ValidationException($"Missing answers for mandatory questions: {string.Join(", ", missing)}");
    }
}
