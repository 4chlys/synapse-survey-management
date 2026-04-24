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
        var questionnaire = await questionnaireManager
            .GetQuestionnaireByQuestionIdWithQuestionsReadOnlyAsync(currentQuestionId);
        if (questionnaire is null) return null;

        var path = await ComputePathAsync(questionnaire, participantId);
        var idx = path.FindIndex(q => q.QuestionId == currentQuestionId);
        if (idx < 0 || idx + 1 >= path.Count) return null;
        return path[idx + 1];
    }

    public async Task<Question> GetPreviousQuestionAsync(int currentQuestionId, int participantId)
    {
        var questionnaire = await questionnaireManager
            .GetQuestionnaireByQuestionIdWithQuestionsReadOnlyAsync(currentQuestionId);
        if (questionnaire is null)
            throw new KeyNotFoundException($"Question with id {currentQuestionId} not found.");

        var path = await ComputePathAsync(questionnaire, participantId);
        var idx = path.FindIndex(q => q.QuestionId == currentQuestionId);
        if (idx <= 0) return null;
        return path[idx - 1];
    }

    public async Task<Question> GetResumeQuestionAsync(int projectId, int participantId)
    {
        var questionnaire = await questionnaireManager
            .GetQuestionnaireByProjectIdWithQuestionsReadOnlyAsync(projectId);
        if (questionnaire is null) return null;

        var path = await ComputePathAsync(questionnaire, participantId);
        if (path.Count == 0) return null;

        var responses = await responseManager.GetResponsesForParticipantReadOnlyAsync(participantId);
        var answeredIds = responses.Select(r => r.Question.QuestionId).ToHashSet();

        return path.FirstOrDefault(q => !answeredIds.Contains(q.QuestionId)) ?? path.LastOrDefault();
    }

    public async Task<List<int>> ComputeVisiblePathAsync(int projectId, int participantId)
    {
        var questionnaire = await questionnaireManager
            .GetQuestionnaireByProjectIdWithQuestionsReadOnlyAsync(projectId);
        if (questionnaire is null) return [];

        var path = await ComputePathAsync(questionnaire, participantId);
        return path.Select(q => q.QuestionId).ToList();
    }

    private async Task<List<Question>> ComputePathAsync(Questionnaire questionnaire, int participantId)
    {
        if (questionnaire?.Questions is null || questionnaire.Questions.Count == 0) return [];

        var allQuestions = questionnaire.Questions.ToList();
        var responses = await responseManager.GetResponsesForParticipantReadOnlyAsync(participantId);
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
                return true;

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
}
