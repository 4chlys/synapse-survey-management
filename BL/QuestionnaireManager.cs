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

    public async Task<IEnumerable<Question>> GetQuestionsAsync()
    {
        return await _repository.ReadAsync<Question>(
            includes: [q => q.Questionnaire]
        );
    }

    public async Task<Questionnaire> GetFirstQuestionnaireAsync()
    {
        return await _repository.ReadSingleAsync<Questionnaire>();
    }

    public async Task<Questionnaire> GetQuestionnaireByProjectIdAsync(int projectId)
    {
        var project = await _repository.ReadSingleAsync<Project>(
            filters: [p => p.ProjectId == projectId],
            includes: [p => p.Questionnaire],
            includeStrings: ["Questionnaire.Questions"]
        );
        return project?.Questionnaire;
    }

    public async Task<Question> GetQuestionByIdAsync(int questionId)
    {
        return await _repository.ReadByIdAsync<Question>(questionId);
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
