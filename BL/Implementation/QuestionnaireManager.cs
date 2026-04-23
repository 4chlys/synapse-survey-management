using System.ComponentModel.DataAnnotations;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.DAL.Interfaces;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.BL.Implementation;

public class QuestionnaireManager(
    IQuestionnaireRepository questionnaireRepository,
    IQuestionRepository questionRepository,
    IResponseRepository responseRepository,
    ISectionRepository sectionRepository,
    IUnitOfWork uow) : IQuestionnaireManager
{
    public async Task<Question> AddQuestionAsync(int projectId, string text,
        QuestionFormat format, bool isMandatory, List<string> answerChoices, int? sectionId = null,
        int? parentQuestionId = null, string parentTriggerValue = null,
        List<string> endingAnswers = null)
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
                question.EndingAnswers.Add(new EndingAnswer { Question = question, Value = value });

        var created = await questionRepository.CreateAsync(question);
        await uow.SaveChangesAsync();
        return created;
    }

    public async Task<List<Question>> GetQuestionsForProjectAsync(int projectId)
    {
        var questions = await questionRepository.ReadByProjectIdWithSectionAndEndingAnswersReadOnlyAsync(projectId);
        return questions.ToList();
    }

    public async Task<bool> UpdateQuestionAsync(int questionId, string text,
        QuestionFormat format, bool isMandatory, List<string> answerChoices, int? sectionId = null,
        List<string> endingAnswers = null)
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

        if (endingAnswers is not null)
        {
            question.EndingAnswers.Clear();
            foreach (var value in endingAnswers)
                question.EndingAnswers.Add(new EndingAnswer { Question = question, Value = value });
        }

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

    public async Task<Question> MoveQuestionAsync(int questionId, int? newParentQuestionId,
        string newParentTriggerValue, int newPosition)
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

        await uow.SaveChangesAsync();
        return question;
    }

    public Task<Questionnaire> GetQuestionnaireByProjectIdAsync(int projectId, bool includeQuestions = true)
        => includeQuestions
            ? questionnaireRepository.ReadByProjectIdWithQuestionsReadOnlyAsync(projectId)
            : questionnaireRepository.ReadByProjectIdReadOnlyAsync(projectId);

    public Task<Question> GetQuestionByIdAsync(int questionId)
        => questionRepository.ReadByIdReadOnlyAsync(questionId);

    public async Task<Question> GetFirstQuestionAsync(int projectId)
    {
        var questionnaire = await GetQuestionnaireByProjectIdAsync(projectId);
        if (questionnaire is null) return null;

        return questionnaire.Questions
            .Where(q => q.ParentQuestion is null)
            .OrderBy(q => q.Position)
            .FirstOrDefault();
    }

    public async Task<Question> GetNextQuestionAsync(int currentQuestionId, int participantId)
    {
        var current = await questionRepository.ReadByIdWithFullQuestionnaireReadOnlyAsync(currentQuestionId);
        if (current is null) return null;

        var path = await ComputeVisiblePathForQuestionnaireAsync(current.Questionnaire, participantId);
        var idx = path.FindIndex(q => q.QuestionId == currentQuestionId);
        if (idx < 0 || idx + 1 >= path.Count) return null;
        return path[idx + 1];
    }

    public async Task<Question> GetPreviousQuestionAsync(int currentQuestionId, int participantId)
    {
        var current = await questionRepository.ReadByIdWithFullQuestionnaireReadOnlyAsync(currentQuestionId);
        if (current is null) throw new KeyNotFoundException($"Question with id {currentQuestionId} not found.");

        var path = await ComputeVisiblePathForQuestionnaireAsync(current.Questionnaire, participantId);
        var idx = path.FindIndex(q => q.QuestionId == currentQuestionId);
        if (idx <= 0) return null;
        return path[idx - 1];
    }

    public async Task<int> GetQuestionCountAsync(int projectId)
    {
        var questionnaire = await GetQuestionnaireByProjectIdAsync(projectId);
        return questionnaire?.Questions.Count ?? 0;
    }

    public async Task<List<int>> ComputeVisiblePathAsync(int projectId, int participantId)
    {
        var questionnaire = await GetQuestionnaireByProjectIdAsync(projectId);
        if (questionnaire is null) return [];

        var path = await ComputeVisiblePathForQuestionnaireAsync(questionnaire, participantId);
        return path.Select(q => q.QuestionId).ToList();
    }

    public async Task<Question> GetResumeQuestionAsync(int projectId, int participantId)
    {
        var questionnaire = await GetQuestionnaireByProjectIdAsync(projectId);
        if (questionnaire is null) return null;

        var path = await ComputeVisiblePathForQuestionnaireAsync(questionnaire, participantId);
        if (path.Count == 0) return null;

        var responses = await responseRepository.ReadByParticipantWithQuestionReadOnlyAsync(participantId);
        var answeredIds = responses.Select(r => r.Question.QuestionId).ToHashSet();

        return path.FirstOrDefault(q => !answeredIds.Contains(q.QuestionId)) ?? path.LastOrDefault();
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

    // Walks the questionnaire honouring nested sub-questions and EndingAnswers.
    // Caller must pass a Questionnaire loaded with Questions + ParentQuestion + EndingAnswers.
    private async Task<List<Question>> ComputeVisiblePathForQuestionnaireAsync(Questionnaire questionnaire, int participantId)
    {
        if (questionnaire?.Questions is null || questionnaire.Questions.Count == 0) return [];

        var allQuestions = questionnaire.Questions.ToList();

        var responses = await responseRepository.ReadByParticipantWithQuestionReadOnlyAsync(participantId);
        var answersById = responses.ToDictionary(r => r.Question.QuestionId, r => r.Value);

        var path = new List<Question>();
        var visited = new HashSet<int>();
        WalkScope(parentId: null, triggerValue: null, allQuestions, answersById, path, visited);
        return path;
    }

    private static bool WalkScope(int? parentId, string triggerValue,
        List<Question> allQuestions,
        Dictionary<int, string> answersById,
        List<Question> path,
        HashSet<int> visited)
    {
        var children = allQuestions
            .Where(q => (q.ParentQuestion?.QuestionId) == parentId
                        && q.ParentTriggerValue == triggerValue)
            .OrderBy(q => q.Position);

        foreach (var child in children)
        {
            if (!visited.Add(child.QuestionId)) continue;
            path.Add(child);

            if (!answersById.TryGetValue(child.QuestionId, out var answer)) continue;

            var selected = SplitSelected(child.QuestionFormat, answer);

            if (child.EndingAnswers.Any(ea => selected.Contains(ea.Value)))
                return true; // survey ends

            foreach (var value in selected)
            {
                var ended = WalkScope(child.QuestionId, value, allQuestions, answersById, path, visited);
                if (ended) return true;
            }
        }
        return false;
    }

    private static IReadOnlyList<string> SplitSelected(QuestionFormat format, string rawAnswer)
    {
        if (string.IsNullOrWhiteSpace(rawAnswer)) return Array.Empty<string>();
        return format == QuestionFormat.MultipleChoice
            ? rawAnswer.Split(", ", StringSplitOptions.RemoveEmptyEntries)
            : new[] { rawAnswer };
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
