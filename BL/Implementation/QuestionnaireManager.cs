using System.ComponentModel.DataAnnotations;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.DAL.Interfaces;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.BL.Implementation;

public class QuestionnaireManager(
    IQuestionnaireRepository questionnaireRepository,
    IQuestionRepository questionRepository,
    ISectionRepository sectionRepository,
    IUnitOfWork uow) : IQuestionnaireManager
{
    public async Task<Question> AddQuestionAsync(int projectId, string text,
        QuestionFormat format, bool isMandatory, List<string> answerChoices, int? sectionId = null)
    {
        var questionnaire = await questionnaireRepository.ReadByProjectIdForUpdateAsync(projectId);
        if (questionnaire is null) return null;

        var question = new Question
        {
            Text = text,
            QuestionFormat = format,
            IsMandatory = isMandatory,
            AnswerChoices = answerChoices ?? new List<string>(),
            Questionnaire = questionnaire,
            Section = sectionId.HasValue
                ? await sectionRepository.ReadByIdForUpdateAsync(sectionId.Value)
                : null
        };
        var created = await questionRepository.CreateAsync(question);
        await uow.SaveChangesAsync();
        return created;
    }

    public Task<IEnumerable<Question>> GetQuestionsForProjectAsync(int projectId)
        => questionRepository.ReadByProjectIdWithSectionAndBranchRulesReadOnlyAsync(projectId);

    public async Task<bool> UpdateQuestionAsync(int questionId, string text,
        QuestionFormat format, bool isMandatory, List<string> answerChoices, int? sectionId = null)
    {
        var question = await questionRepository.ReadByIdWithSectionForUpdateAsync(questionId);
        if (question is null) return false;

        question.Text = text;
        question.QuestionFormat = format;
        question.IsMandatory = isMandatory;
        question.AnswerChoices = answerChoices ?? new List<string>();
        question.Section = sectionId.HasValue
            ? await sectionRepository.ReadByIdForUpdateAsync(sectionId.Value)
            : null;
        questionRepository.Update(question);
        await uow.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteQuestionAsync(int questionId)
    {
        var result = await questionRepository.DeleteByIdAsync(questionId);
        if (result) await uow.SaveChangesAsync();
        return result;
    }

    public Task<Questionnaire> GetQuestionnaireByProjectIdWithQuestionsReadOnlyAsync(int projectId)
        => questionnaireRepository.ReadByProjectIdWithQuestionsReadOnlyAsync(projectId);

    public Task<Questionnaire> GetQuestionnaireByProjectIdReadOnlyAsync(int projectId)
        => questionnaireRepository.ReadByProjectIdReadOnlyAsync(projectId);

    public Task<Questionnaire> GetQuestionnaireByProjectIdForUpdateAsync(int projectId)
        => questionnaireRepository.ReadByProjectIdForUpdateAsync(projectId);

    public Task<Question> GetQuestionByIdReadOnlyAsync(int questionId)
        => questionRepository.ReadByIdReadOnlyAsync(questionId);

    public Task<Question> GetQuestionByIdWithBranchRulesAndQuestionnaireReadOnlyAsync(int questionId)
        => questionRepository.ReadByIdWithBranchRulesAndQuestionnaireReadOnlyAsync(questionId);

    public Task<Question> GetQuestionByIdWithFullQuestionnaireReadOnlyAsync(int questionId)
        => questionRepository.ReadByIdWithFullQuestionnaireReadOnlyAsync(questionId);

    public Task<Questionnaire> GetQuestionnaireByQuestionIdWithQuestionsReadOnlyAsync(int questionId)
        => questionnaireRepository.ReadByQuestionIdWithQuestionsReadOnlyAsync(questionId);

    public async Task<Question> GetFirstQuestionAsync(int projectId)
    {
        var questionnaire = await GetQuestionnaireByProjectIdWithQuestionsReadOnlyAsync(projectId);
        if (questionnaire is null) return null;

        return questionnaire.Questions
            .OrderBy(q => q.Position)
            .FirstOrDefault();
    }

    public async Task<int> GetQuestionCountAsync(int projectId)
    {
        var questionnaire = await GetQuestionnaireByProjectIdWithQuestionsReadOnlyAsync(projectId);
        return questionnaire?.Questions.Count ?? 0;
    }

    public void ValidateMandatoryAnswers(Questionnaire questionnaire,
        IEnumerable<(int QuestionId, string Value)> answers,
        HashSet<int> visibleQuestionIds = null)
    {
        var answeredIds = answers
            .Where(a => !string.IsNullOrWhiteSpace(a.Value))
            .Select(a => a.QuestionId)
            .ToHashSet();

        var questionsToValidate = questionnaire.Questions
            .Where(q => q.IsMandatory);

        if (visibleQuestionIds is not null)
            questionsToValidate = questionsToValidate.Where(q => visibleQuestionIds.Contains(q.QuestionId));

        var missing = questionsToValidate
            .Where(q => !answeredIds.Contains(q.QuestionId))
            .Select(q => q.QuestionId)
            .ToList();

        if (missing.Count > 0)
            throw new ValidationException($"Missing answers for mandatory questions: {string.Join(", ", missing)}");
    }

    public Task<IEnumerable<Section>> GetSectionsReadOnlyAsync(int projectId)
        => sectionRepository.ReadByProjectIdReadOnlyAsync(projectId);

    public async Task<Section> CreateSectionAsync(int projectId, string name, string description)
    {
        var questionnaire = await questionnaireRepository.ReadByProjectIdForUpdateAsync(projectId);
        if (questionnaire is null) return null;

        var section = new Section
        {
            Name = name,
            Description = description,
            Questionnaire = questionnaire
        };
        var created = await sectionRepository.CreateAsync(section);
        await uow.SaveChangesAsync();
        return created;
    }

    public async Task<bool> UpdateSectionAsync(int sectionId, string name, string description)
    {
        var section = await sectionRepository.ReadByIdForUpdateAsync(sectionId);
        if (section is null) return false;

        section.Name = name;
        section.Description = description;
        sectionRepository.Update(section);
        await uow.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteSectionAsync(int sectionId)
    {
        var result = await sectionRepository.DeleteByIdAsync(sectionId);
        if (result) await uow.SaveChangesAsync();
        return result;
    }
}
