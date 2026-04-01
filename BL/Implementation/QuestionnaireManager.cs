using System.ComponentModel.DataAnnotations;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.DAL;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.BL.Implementation;

public class QuestionnaireManager(IRepository repository) : IQuestionnaireManager
{

    public async Task<Question> AddQuestionAsync(int projectId, string text,
        QuestionFormat format, bool isMandatory, List<string> answerChoices)
    {
        var questionnaire = await GetQuestionnaireByProjectIdAsync(projectId,
            includeQuestions: false);
        if (questionnaire is null) return null;

        var question = new Question
        {
            Text = text,
            QuestionFormat = format,
            IsMandatory = isMandatory,
            AnswerChoices = answerChoices ?? new List<string>(),
            Questionnaire = questionnaire
        };
        return await repository.CreateAsync(question);
    }
    public async Task<bool> UpdateQuestionAsync(int questionId, string text,
        QuestionFormat format, bool isMandatory, List<string> answerChoices)
    {
        var question = await repository.ReadByIdAsync<Question>(questionId);
        if (question is null) return false;

        question.Text = text;
        question.QuestionFormat = format;
        question.IsMandatory = isMandatory;
        question.AnswerChoices = answerChoices ?? new List<string>();
        return await repository.UpdateAsync(question);
    }

    public async Task<bool> DeleteQuestionAsync(int questionId)
    {
        return await repository.DeleteByIdAsync<Question>(questionId);
    }
    public async Task<Questionnaire> GetQuestionnaireByProjectIdAsync(int projectId, bool includeQuestions = true)
    {
        return await repository.ReadSingleAsync<Questionnaire>(
            filters: [q => q.Project.ProjectId == projectId],
            includeStrings: includeQuestions ? ["Questions"] : null
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