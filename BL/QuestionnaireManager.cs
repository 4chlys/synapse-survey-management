using System.ComponentModel.DataAnnotations;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.DAL;

namespace SynapseEnqueteManagement.BL;

public class QuestionnaireManager(IRepository repository) : IQuestionnaireManager
{
    public async Task<IEnumerable<Question>> GetQuestionsAsync()
    {
        return await repository.ReadAsync<Question>(
            includes: [q => q.Questionnaire]
        );
    }

    public async Task<Questionnaire> GetFirstQuestionnaireAsync()
    {
        return await repository.ReadSingleAsync<Questionnaire>();
    }

    public async Task<Questionnaire> GetQuestionnaireByProjectIdAsync(int projectId)
    {
        var project = await repository.ReadSingleAsync<Project>(
            filters: [p => p.ProjectId == projectId],
            includes: [p => p.Questionnaire],
            includeStrings: ["Questionnaire.Questions"]
        );
        return project?.Questionnaire;
    }

    public async Task<Question> GetQuestionByIdAsync(int questionId)
    {
        return await repository.ReadByIdAsync<Question>(questionId);
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
