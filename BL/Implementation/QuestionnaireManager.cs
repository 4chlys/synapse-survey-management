using System.ComponentModel.DataAnnotations;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.DAL;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.BL.Implementation;

public class QuestionnaireManager(IRepository repository) : IQuestionnaireManager
{
    public async Task<Questionnaire> GetQuestionnaireByProjectIdAsync(int projectId)
    {
        return await repository.ReadSingleAsync<Questionnaire>(
            filters: [q => q.Project.ProjectId == projectId],
            includeStrings: ["Questions"]
        );
    }

    public async Task<Question> GetQuestionByIdAsync(int questionId)
    {
        return await repository.ReadByIdAsync<Question>(questionId);
    }

    public void ValidateMandatoryAnswers(Questionnaire questionnaire,
        IEnumerable<(int QuestionId, string Value)> answers)
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