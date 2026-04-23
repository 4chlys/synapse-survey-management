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
        QuestionFormat format, bool isMandatory, List<string> answerChoices, int? sectionId = null)
    {
        var questionnaire = await questionnaireRepository.ReadByProjectIdForUpdateAsync(projectId);
        if (questionnaire is null) return null;

        var section = sectionId.HasValue
            ? await sectionRepository.ReadByIdForUpdateAsync(sectionId.Value)
            : null;

        var question = new Question
        {
            Text = text,
            QuestionFormat = format,
            IsMandatory = isMandatory,
            AnswerChoices = answerChoices ?? new List<string>(),
            Questionnaire = questionnaire,
            Section = section
        };
        var created = await questionRepository.CreateAsync(question);
        await uow.SaveChangesAsync();
        return created;
    }

    public async Task<List<Question>> GetQuestionsForProjectAsync(int projectId)
    {
        var questions = await questionRepository.ReadByProjectIdWithSectionAndBranchRulesReadOnlyAsync(projectId);
        return questions.ToList();
    }

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
            .OrderBy(q => q.Position)
            .FirstOrDefault();
    }

    public async Task<Question> GetNextQuestionAsync(int currentQuestionId, int participantId)
    {
        var currentQuestion = await questionRepository.ReadByIdWithBranchRulesAndQuestionnaireReadOnlyAsync(currentQuestionId);
        if (currentQuestion is null) return null;

        var response = await responseRepository.ReadByQuestionAndParticipantReadOnlyAsync(currentQuestionId, participantId);

        if (response is not null && currentQuestion.BranchRules.Count > 0)
        {
            var matchingRule = FindMatchingBranchRule(currentQuestion, response.Value);
            if (matchingRule is not null)
            {
                if (matchingRule.SkipToEnd) return null;
                return matchingRule.TargetQuestion;
            }
        }

        var allQuestions = await GetQuestionsForQuestionnaire(currentQuestion);
        return allQuestions
            .Where(q => q.Position > currentQuestion.Position)
            .OrderBy(q => q.Position)
            .FirstOrDefault();
    }

    public async Task<Question> GetPreviousQuestionAsync(int currentQuestionId, int participantId)
    {
        var current = await questionRepository.ReadByIdWithFullQuestionnaireReadOnlyAsync(currentQuestionId);
        if (current is null) throw new KeyNotFoundException($"Question with id {currentQuestionId} not found.");

        var allQuestions = current.Questionnaire.Questions;

        var branchSources = allQuestions
            .Where(q => q.BranchRules.Any(b => b.TargetQuestion?.QuestionId == currentQuestionId))
            .ToList();

        foreach (var branchSource in branchSources)
        {
            var response = await responseRepository.ReadByQuestionAndParticipantReadOnlyAsync(branchSource.QuestionId, participantId);
            if (response is null) continue;

            var matchedRule = FindMatchingBranchRule(branchSource, response.Value);
            var ruleWasTaken = matchedRule?.TargetQuestion?.QuestionId == currentQuestionId;

            if (ruleWasTaken)
                return branchSource;
        }

        // Linear fallback: nearest question with a lower position
        return allQuestions
            .Where(q => q.Position < current.Position)
            .OrderByDescending(q => q.Position)
            .FirstOrDefault();
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

        var firstQuestion = questionnaire.Questions.OrderBy(q => q.Position).FirstOrDefault();
        if (firstQuestion is null) return [];

        var path = await ComputeVisiblePathInternal(firstQuestion, participantId);
        return path.Select(q => q.QuestionId).ToList();
    }

    public async Task<Question> GetResumeQuestionAsync(int projectId, int participantId)
    {
        var questionnaire = await GetQuestionnaireByProjectIdAsync(projectId);
        if (questionnaire is null) return null;

        var firstQuestion = questionnaire.Questions.OrderBy(q => q.Position).FirstOrDefault();
        if (firstQuestion is null) return null;

        var path = await ComputeVisiblePathInternal(firstQuestion, participantId);

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

    private async Task<List<Question>> ComputeVisiblePathInternal(Question startQuestion, int participantId)
    {
        var allQuestions = await GetQuestionsForQuestionnaire(startQuestion);

        var responses = await responseRepository.ReadByParticipantWithQuestionReadOnlyAsync(participantId);
        var answersByQuestionId = responses.ToDictionary(r => r.Question.QuestionId, r => r.Value);

        var path = new List<Question>();
        var visited = new HashSet<int>();
        var current = startQuestion;

        while (current is not null)
        {
            if (!visited.Add(current.QuestionId)) break;

            path.Add(current);

            if (answersByQuestionId.TryGetValue(current.QuestionId, out var answer)
                && current.BranchRules.Count > 0)
            {
                var matchingRule = FindMatchingBranchRule(current, answer);
                if (matchingRule is not null)
                {
                    if (matchingRule.SkipToEnd) break;
                    current = allQuestions.FirstOrDefault(q => q.QuestionId == matchingRule.TargetQuestion?.QuestionId);
                    continue;
                }
            }

            current = allQuestions
                .Where(q => q.Position > current.Position)
                .OrderBy(q => q.Position)
                .FirstOrDefault();
        }

        return path;
    }

    private async Task<List<Question>> GetQuestionsForQuestionnaire(Question question)
    {
        var questionnaire = await questionnaireRepository.ReadByQuestionIdWithQuestionsReadOnlyAsync(question.QuestionId);
        return questionnaire?.Questions.OrderBy(q => q.Position).ToList() ?? [];
    }

    private static BranchRule FindMatchingBranchRule(Question question, string answer)
    {
        if (string.IsNullOrWhiteSpace(answer)) return null;

        foreach (var rule in question.BranchRules)
        {
            if (question.QuestionFormat == QuestionFormat.MultipleChoice)
            {
                var selectedValues = answer.Split(", ", StringSplitOptions.RemoveEmptyEntries);
                if (selectedValues.Contains(rule.TriggerValue))
                    return rule;
            }
            else
            {
                if (string.Equals(rule.TriggerValue, answer, StringComparison.OrdinalIgnoreCase))
                    return rule;
            }
        }

        return null;
    }
}
