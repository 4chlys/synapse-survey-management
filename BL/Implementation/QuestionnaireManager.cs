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
    public Task<Questionnaire> GetQuestionnaireByProjectIdWithQuestionsReadOnlyAsync(int projectId)
        => questionnaireRepository.ReadByProjectIdWithQuestionsReadOnlyAsync(projectId);

    public Task<Questionnaire> GetQuestionnaireByProjectIdReadOnlyAsync(int projectId)
        => questionnaireRepository.ReadByProjectIdReadOnlyAsync(projectId);

    public Task<Questionnaire> GetQuestionnaireByQuestionIdWithQuestionsReadOnlyAsync(int questionId)
        => questionnaireRepository.ReadByQuestionIdWithQuestionsReadOnlyAsync(questionId);

    public Task<Question> GetQuestionByIdReadOnlyAsync(int questionId)
        => questionRepository.ReadByIdReadOnlyAsync(questionId);

    public Task<bool> QuestionBelongsToProjectAsync(int questionId, int projectId)
        => questionRepository.ExistsForProjectAsync(questionId, projectId);

    public async Task<Question> AddQuestionAsync(int projectId, string text,
        QuestionFormat format, bool isMandatory, List<string> answerChoices, int? sectionId = null,
        int? parentQuestionId = null, string parentTriggerValue = null,
        List<string> endingAnswers = null, CancellationToken ct = default)
    {
        var questionnaire = await questionnaireRepository.ReadByProjectIdForUpdateAsync(projectId);
        if (questionnaire is null) return null;

        var section = sectionId.HasValue
            ? await sectionRepository.ReadByIdForUpdateAsync(sectionId.Value)
            : null;

        Question parent = null;
        if (parentQuestionId.HasValue)
        {
            parent = await questionRepository.ReadByIdWithSectionForUpdateAsync(parentQuestionId.Value);
            if (parent is null)
                throw new ValidationException($"Parent question {parentQuestionId} not found.");
            EntityValidator.ValidateProperty(new Question(), parentTriggerValue, nameof(Question.ParentTriggerValue));
            ValidateParentTrigger(parent, parentTriggerValue);
        }
        else if (!string.IsNullOrEmpty(parentTriggerValue))
        {
            throw new ValidationException("ParentTriggerValue must be null when no parent is set.");
        }

        var siblings = (await questionRepository.ReadSiblingsForUpdateAsync(
            parentQuestionId, parentTriggerValue, questionnaire.QuestionnaireId)).ToList();
        var nextPosition = siblings.Count == 0 ? 1 : siblings.Max(s => s.Position) + 1;

        var question = new Question
        {
            Text = text,
            QuestionFormat = format,
            IsMandatory = isMandatory,
            AnswerChoices = answerChoices ?? new List<string>(),
            Questionnaire = questionnaire,
            Section = section,
            ParentQuestion = parent,
            ParentTriggerValue = parent is not null ? parentTriggerValue : null,
            Position = nextPosition
        };
        if (endingAnswers is not null)
            foreach (var value in endingAnswers)
            {
                EntityValidator.ValidateProperty(new EndingAnswer(), value, nameof(EndingAnswer.Value));
                question.EndingAnswers.Add(new EndingAnswer { Question = question, Value = value });
            }
        EntityValidator.Validate(question);
        var created = await questionRepository.CreateAsync(question);
        await uow.SaveChangesAsync(ct);
        return created;
    }

    public async Task<IEnumerable<Question>> GetQuestionsForProjectAsync(int projectId)
        => await questionRepository.ReadByProjectIdWithSectionAndEndingAnswersReadOnlyAsync(projectId);

    public async Task<bool> UpdateQuestionAsync(int questionId, string text,
        QuestionFormat format, bool isMandatory, List<string> answerChoices, int? sectionId = null,
        List<string> endingAnswers = null, CancellationToken ct = default)
    {
        var question = await questionRepository.ReadByIdWithSectionForUpdateAsync(questionId);
        if (question is null) return false;

        EntityValidator.ValidateProperty(question, text, nameof(Question.Text));
        question.Text = text;
        question.QuestionFormat = format;
        question.IsMandatory = isMandatory;
        question.AnswerChoices = answerChoices ?? new List<string>();
        question.Section = sectionId.HasValue
            ? await sectionRepository.ReadByIdForUpdateAsync(sectionId.Value)
            : null;

        if (endingAnswers is not null)
        {
            question.EndingAnswers.Clear();
            foreach (var value in endingAnswers)
            {
                EntityValidator.ValidateProperty(new EndingAnswer(), value, nameof(EndingAnswer.Value));
                question.EndingAnswers.Add(new EndingAnswer { Question = question, Value = value });
            }
        }

        questionRepository.Update(question);
        await uow.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteQuestionAsync(int questionId, CancellationToken ct = default)
    {
        var result = await questionRepository.DeleteByIdAsync(questionId);
        if (result) await uow.SaveChangesAsync(ct);
        return result;
    }

    public async Task<Question> MoveQuestionAsync(int questionId, int? newParentQuestionId,
        string newParentTriggerValue, int newPosition, CancellationToken ct = default)
    {
        var question = await questionRepository.ReadByIdWithParentChainForUpdateAsync(questionId);
        if (question is null) return null;

        Question newParent = null;
        if (newParentQuestionId.HasValue)
        {
            if (newParentQuestionId.Value == questionId)
                throw new ValidationException("A question cannot be its own parent.");

            newParent = await questionRepository.ReadByIdWithParentChainForUpdateAsync(newParentQuestionId.Value);
            if (newParent is null)
                throw new ValidationException($"Parent question {newParentQuestionId} not found.");
            if (newParent.Questionnaire.QuestionnaireId != question.Questionnaire.QuestionnaireId)
                throw new ValidationException("Parent must belong to the same questionnaire.");

            ValidateParentTrigger(newParent, newParentTriggerValue);
            EnsureNoCycle(question, newParent);
        }
        else if (!string.IsNullOrEmpty(newParentTriggerValue))
        {
            throw new ValidationException("ParentTriggerValue must be null when moving to top-level.");
        }

        var oldParentId = question.ParentQuestion?.QuestionId;
        var oldTrigger = question.ParentTriggerValue;
        var questionnaireId = question.Questionnaire.QuestionnaireId;

        question.ParentQuestion = newParent;
        question.ParentTriggerValue = newParent is null ? null : newParentTriggerValue;
        questionRepository.Update(question);

        await RepackScopeAsync(questionnaireId, oldParentId, oldTrigger, excludeId: questionId);
        await RepackScopeAsync(questionnaireId, newParentQuestionId, newParent is null ? null : newParentTriggerValue,
            insertId: questionId, insertAt: newPosition);

        await uow.SaveChangesAsync(ct);
        return question;
    }

    public async Task<Question> GetFirstQuestionAsync(int projectId)
    {
        var questionnaire = await questionnaireRepository.ReadByProjectIdWithQuestionsReadOnlyAsync(projectId);
        if (questionnaire is null) return null;

        return questionnaire.Questions
            .Where(q => q.ParentQuestion is null)
            .OrderBy(q => q.Position)
            .FirstOrDefault();
    }

    public async Task<int> GetQuestionCountAsync(int projectId)
    {
        var questionnaire = await questionnaireRepository.ReadByProjectIdWithQuestionsReadOnlyAsync(projectId);
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

    public async Task<Section> CreateSectionAsync(int projectId, string name, string description, CancellationToken ct = default)
    {
        var questionnaire = await questionnaireRepository.ReadByProjectIdForUpdateAsync(projectId);
        if (questionnaire is null) return null;

        var section = new Section
        {
            Name = name,
            Description = description,
            Questionnaire = questionnaire
        };
        EntityValidator.Validate(section);
        var created = await sectionRepository.CreateAsync(section);
        await uow.SaveChangesAsync(ct);
        return created;
    }

    public async Task<bool> UpdateSectionAsync(int sectionId, string name, string description, CancellationToken ct = default)
    {
        var section = await sectionRepository.ReadByIdForUpdateAsync(sectionId);
        if (section is null) return false;

        EntityValidator.ValidateProperty(section, name, nameof(Section.Name));
        EntityValidator.ValidateProperty(section, description, nameof(Section.Description));
        section.Name = name;
        section.Description = description;
        sectionRepository.Update(section);
        await uow.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteSectionAsync(int sectionId, CancellationToken ct = default)
    {
        var result = await sectionRepository.DeleteByIdAsync(sectionId);
        if (result) await uow.SaveChangesAsync(ct);
        return result;
    }

    private static void ValidateParentTrigger(Question parent, string triggerValue)
    {
        if (parent.QuestionFormat == QuestionFormat.Open)
            throw new ValidationException("Open questions cannot have sub-questions.");
        if (string.IsNullOrEmpty(triggerValue))
            throw new ValidationException("ParentTriggerValue is required when nesting under a parent.");

        if (parent.QuestionFormat == QuestionFormat.SingleChoice
            || parent.QuestionFormat == QuestionFormat.MultipleChoice)
        {
            if (!parent.AnswerChoices.Contains(triggerValue))
                throw new ValidationException(
                    $"TriggerValue '{triggerValue}' is not one of the parent's answer choices.");
        }
        // Likert: accept any integer — the UI presents 1..N and we don't currently store Min/Max on Question.
        // If Likert bounds are later added to the domain, validate them here.
    }

    private static void EnsureNoCycle(Question moving, Question newParent)
    {
        var cursor = newParent;
        while (cursor is not null)
        {
            if (cursor.QuestionId == moving.QuestionId)
                throw new ValidationException("Move would create a cycle.");
            cursor = cursor.ParentQuestion;
        }
    }

    private async Task RepackScopeAsync(int questionnaireId, int? parentQuestionId, string triggerValue,
        int? excludeId = null, int? insertId = null, int? insertAt = null)
    {
        var siblings = (await questionRepository.ReadSiblingsForUpdateAsync(
            parentQuestionId, triggerValue, questionnaireId)).ToList();

        if (excludeId.HasValue)
            siblings.RemoveAll(s => s.QuestionId == excludeId.Value);

        if (insertId.HasValue)
        {
            siblings.RemoveAll(s => s.QuestionId == insertId.Value);
            var target = await questionRepository.ReadByIdWithSectionForUpdateAsync(insertId.Value);
            if (target is not null)
            {
                var clamped = Math.Clamp(insertAt ?? siblings.Count, 0, siblings.Count);
                siblings.Insert(clamped, target);
            }
        }

        for (var i = 0; i < siblings.Count; i++)
        {
            var desired = i + 1;
            if (siblings[i].Position != desired)
            {
                siblings[i].Position = desired;
                questionRepository.Update(siblings[i]);
            }
        }
    }
}
