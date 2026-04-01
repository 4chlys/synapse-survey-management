using System.ComponentModel.DataAnnotations;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.DAL;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.BL.Implementation;

public class QuestionnaireManager(IRepository repository) : IQuestionnaireManager
{
    public async Task<Questionnaire> GetQuestionnaireByProjectIdAsync(int projectId, bool includeQuestions = true)
    {
        return await repository.ReadSingleAsync<Questionnaire>(
            filters: [q => q.Project.ProjectId == projectId],
            includeStrings: includeQuestions ? ["Questions.BranchRules"] : null
        );
    }

    public async Task<Question> GetQuestionByIdAsync(int questionId)
    {
        return await repository.ReadByIdAsync<Question>(questionId);
    }

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
        var currentQuestion = await repository.ReadByIdAsync<Question>(
            currentQuestionId,
            includeStrings: ["BranchRules.TargetQuestion", "Questionnaire"]);

        if (currentQuestion is null) return null;

        var response = await repository.ReadSingleAsync<Response>(
            filters:
            [
                r => r.Question.QuestionId == currentQuestionId,
                r => r.Participant.ParticipantId == participantId
            ]);

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
        var current = await repository.ReadByIdAsync<Question>(
            currentQuestionId,
            includeStrings: ["Questionnaire.Questions.BranchRules.TargetQuestion"],
            tracked: true);
        
        if (current is null) throw new KeyNotFoundException($"Question with id {currentQuestionId} not found.");

        var allQuestions = current.Questionnaire.Questions;

        var branchSources = allQuestions
            .Where(q => q.BranchRules.Any(b => b.TargetQuestion?.QuestionId == currentQuestionId))
            .ToList();

        foreach (var branchSource in branchSources)
        {
            var response = await repository.ReadSingleAsync<Response>(
                filters: [r => r.Participant.ParticipantId == participantId
                               && r.Question.QuestionId == branchSource.QuestionId]
            );

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

        var responses = await repository.ReadAsync<Response>(
            filters: [r => r.Participant.ParticipantId == participantId],
            includeStrings: ["Question"]);

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

        var responses = await repository.ReadAsync<Response>(
            filters: [r => r.Participant.ParticipantId == participantId],
            includeStrings: ["Question"]);

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
        var questionnaire = await repository.ReadSingleAsync<Questionnaire>(
            filters: [q => q.Questions.Any(qu => qu.QuestionId == question.QuestionId)],
            includeStrings: ["Questions.BranchRules.TargetQuestion"]);

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
