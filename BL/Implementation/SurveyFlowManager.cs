using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.BL.Helpers;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.BL.Implementation;

public class SurveyFlowManager(
    IQuestionnaireManager questionnaireManager,
    IResponseManager responseManager) : ISurveyFlowManager
{
    public async Task<Question> GetNextQuestionAsync(int currentQuestionId, int participantId)
    {
        var questionnaire = await questionnaireManager
            .GetQuestionnaireByQuestionIdWithTranslationsReadOnlyAsync(currentQuestionId);
        if (questionnaire is null) return null;

        var path = await ComputePathAsync(questionnaire, participantId);
        var idx = path.FindIndex(q => q.QuestionId == currentQuestionId);
        if (idx < 0 || idx + 1 >= path.Count) return null;
        return path[idx + 1];
    }

    public async Task<Question> GetPreviousQuestionAsync(int currentQuestionId, int participantId)
    {
        var questionnaire = await questionnaireManager
            .GetQuestionnaireByQuestionIdWithTranslationsReadOnlyAsync(currentQuestionId);
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
            .GetQuestionnaireByProjectIdWithTranslationsReadOnlyAsync(projectId);
        if (questionnaire is null) return null;

        var path = await ComputePathAsync(questionnaire, participantId);
        if (path.Count == 0) return null;

        var resumeResponses = await responseManager.GetResponsesForParticipantReadOnlyAsync(participantId);
        var answeredIds = resumeResponses.Select(r => r.Question.QuestionId).ToHashSet();

        return path.FirstOrDefault(q => !answeredIds.Contains(q.QuestionId)) ?? path.LastOrDefault();
    }

    public async Task<ICollection<int>> ComputeVisiblePathAsync(int projectId, int participantId)
    {
        var questionnaire = await questionnaireManager
            .GetQuestionnaireByProjectIdWithTranslationsReadOnlyAsync(projectId);
        if (questionnaire is null) return [];

        var path = await ComputePathAsync(questionnaire, participantId);
        return path.ConvertAll(q => q.QuestionId);
    }

    private async Task<List<Question>> ComputePathAsync(Questionnaire questionnaire, int participantId)
    {
        if (questionnaire?.Questions is null || questionnaire.Questions.Count == 0) return [];

        var allQuestions = questionnaire.Questions.ToList();
        var responses = await responseManager.GetResponsesForParticipantReadOnlyAsync(participantId);
        // Each response carries its own source language (the participant's language at save time),
        // so branching/ending lookups must use that language — not the participant's current one.
        // Defensive GroupBy: a unique index on (QuestionId, ParticipantId) prevents duplicates at the
        // schema level, but if a race or backfill ever inserts two rows we take the first rather than
        // crash the survey flow.
        var responseByQId = responses
            .GroupBy(r => r.Question.QuestionId)
            .ToDictionary(g => g.Key, g => g.First());

        var path = new List<Question>();
        var visited = new HashSet<int>();
        WalkScope(parentId: null, triggerChoiceIndex: null,
            allQuestions, responseByQId, path, visited);
        return path;
    }

    private static bool WalkScope(
        int? parentId, int? triggerChoiceIndex,
        List<Question> allQuestions,
        Dictionary<int, Response> responseByQId,
        List<Question> path,
        HashSet<int> visited)
    {
        var children = allQuestions
            .Where(q => (q.ParentQuestion?.QuestionId) == parentId
                        && q.ChoiceIndex == triggerChoiceIndex)
            .OrderBy(q => q.Position);

        foreach (var child in children)
        {
            if (!visited.Add(child.QuestionId)) continue;
            path.Add(child);

            if (!responseByQId.TryGetValue(child.QuestionId, out var response)) continue;

            var selected = SplitSelected(child.QuestionFormat, response.Value);
            var sourceLanguageCode = response.Language?.Code;

            // Check if the selected answer terminates the survey (match by translated value
            // in the response's source language, falling back to first translation)
            if (child.EndingAnswers.Any(ea =>
                {
                    var endVal = TranslationPicker.Pick(ea.Translations, sourceLanguageCode)?.Value;
                    return endVal is not null && selected.Contains(endVal);
                }))
                return true;

            // Resolve selected answer strings to choice indices for branching, using the
            // response's source language so saved values map to the correct choice index.
            var choices = TranslationPicker.Pick(child.Translations, sourceLanguageCode)?.AnswerChoices?.ToList()
                          ?? new List<string>();

            foreach (var value in selected)
            {
                int idx;
                if (child.QuestionFormat == QuestionFormat.Likert
                    && int.TryParse(value, out var raw))
                {
                    // Likert responses are 1-based; ChoiceIndex is 0-based
                    idx = raw - 1;
                }
                else
                {
                    idx = choices.IndexOf(value);
                    if (idx < 0) continue;
                }

                var ended = WalkScope(child.QuestionId, idx,
                    allQuestions, responseByQId, path, visited);
                if (ended) return true;
            }
        }
        return false;
    }

    private static IReadOnlyList<string> SplitSelected(QuestionFormat format, string rawAnswer)
    {
        if (string.IsNullOrWhiteSpace(rawAnswer)) return Array.Empty<string>();
        return format == QuestionFormat.MultipleChoice
            ? MultipleChoiceValueSerializer.Split(rawAnswer)
            : new[] { rawAnswer };
    }
}
