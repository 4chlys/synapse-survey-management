using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.BL.Implementation;

public class SurveyFlowManager(
    IQuestionnaireManager questionnaireManager,
    IResponseManager responseManager) : ISurveyFlowManager
{
    public async Task<Question> GetNextQuestionAsync(int currentQuestionId, int participantId)
    {
        var currentQuestion = await questionnaireManager.GetQuestionByIdWithBranchRulesAndQuestionnaireReadOnlyAsync(currentQuestionId);
        if (currentQuestion is null) return null;

        var response = await responseManager.GetResponseByQuestionIdAndParticipantIdReadOnlyAsync(currentQuestionId, participantId);

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
        var current = await questionnaireManager.GetQuestionByIdReadOnlyAsync(currentQuestionId);
        if (current is null) throw new KeyNotFoundException($"Question with id {currentQuestionId} not found.");

        var questionnaire = await questionnaireManager.GetQuestionnaireByQuestionIdWithQuestionsReadOnlyAsync(currentQuestionId);
        if (questionnaire is null) throw new KeyNotFoundException($"Questionnaire for question {currentQuestionId} not found.");

        var allQuestions = questionnaire.Questions;

        var branchSources = allQuestions
            .Where(q => q.BranchRules.Any(b => b.TargetQuestion?.QuestionId == currentQuestionId))
            .ToList();

        foreach (var branchSource in branchSources)
        {
            var response = await responseManager.GetResponseByQuestionIdAndParticipantIdReadOnlyAsync(branchSource.QuestionId, participantId);
            if (response is null) continue;

            var matchedRule = FindMatchingBranchRule(branchSource, response.Value);
            if (matchedRule?.TargetQuestion?.QuestionId == currentQuestionId)
                return branchSource;
        }

        return allQuestions
            .Where(q => q.Position < current.Position)
            .OrderByDescending(q => q.Position)
            .FirstOrDefault();
    }

    public async Task<Question> GetResumeQuestionAsync(int projectId, int participantId)
    {
        var questionnaire = await questionnaireManager.GetQuestionnaireByProjectIdWithQuestionsReadOnlyAsync(projectId);
        if (questionnaire is null) return null;

        var firstQuestion = questionnaire.Questions.OrderBy(q => q.Position).FirstOrDefault();
        if (firstQuestion is null) return null;

        var path = await ComputeVisiblePathInternal(firstQuestion, participantId);

        var responses = await responseManager.GetResponsesForParticipantReadOnlyAsync(participantId);
        var answeredIds = responses.Select(r => r.Question.QuestionId).ToHashSet();

        return path.FirstOrDefault(q => !answeredIds.Contains(q.QuestionId)) ?? path.LastOrDefault();
    }

    public async Task<IEnumerable<int>> ComputeVisiblePathAsync(int projectId, int participantId)
    {
        var questionnaire = await questionnaireManager.GetQuestionnaireByProjectIdWithQuestionsReadOnlyAsync(projectId);
        if (questionnaire is null) return [];

        var firstQuestion = questionnaire.Questions.OrderBy(q => q.Position).FirstOrDefault();
        if (firstQuestion is null) return [];

        var path = await ComputeVisiblePathInternal(firstQuestion, participantId);
        return path.Select(q => q.QuestionId);
    }

    private async Task<List<Question>> ComputeVisiblePathInternal(Question startQuestion, int participantId)
    {
        var allQuestions = await GetQuestionsForQuestionnaire(startQuestion);

        var responses = await responseManager.GetResponsesForParticipantReadOnlyAsync(participantId);
        var answersByQuestionId = responses.ToDictionary(r => r.Question.QuestionId, r => r.Value);

        var path = new List<Question>();
        var visited = new HashSet<int>();
        var current = allQuestions.FirstOrDefault(q => q.QuestionId == startQuestion.QuestionId);
        if (current is null) return path;

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
        var questionnaire = await questionnaireManager.GetQuestionnaireByQuestionIdWithQuestionsReadOnlyAsync(question.QuestionId);
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
