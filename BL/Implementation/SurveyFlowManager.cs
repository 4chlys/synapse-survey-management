using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.BL.Implementation;

public class SurveyFlowManager(
    IQuestionnaireManager questionnaireManager,
    IResponseManager responseManager,
    IParticipantManager participantManager) : ISurveyFlowManager
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

        var responses = await responseManager.GetResponsesForParticipantReadOnlyAsync(participantId);
        var answeredIds = responses.Select(r => r.Question.QuestionId).ToHashSet();

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

        var languageCode = await participantManager.GetParticipantLanguageCodeAsync(participantId) ?? "nl";

        var allQuestions = questionnaire.Questions.ToList();
        var responses = await responseManager.GetResponsesForParticipantReadOnlyAsync(participantId);
        var answersById = responses.ToDictionary(r => r.Question.QuestionId, r => r.Value);

        // Resolve each question's translation for the participant's language (fallback to first available)
        var translationByQId = allQuestions.ToDictionary(
            q => q.QuestionId,
            q => q.Translations.FirstOrDefault(t => t.Language.Code == languageCode)
                 ?? q.Translations.FirstOrDefault());

        // Resolve each ending answer's translated value for the participant's language
        var endingValueById = allQuestions
            .SelectMany(q => q.EndingAnswers)
            .ToDictionary(
                ea => ea.EndingAnswerId,
                ea => ea.Translations.FirstOrDefault(t => t.Language.Code == languageCode)?.Value
                      ?? ea.Translations.FirstOrDefault()?.Value);

        var path = new List<Question>();
        var visited = new HashSet<int>();
        WalkScope(parentId: null, triggerChoiceIndex: null,
            allQuestions, answersById, translationByQId, endingValueById, path, visited);
        return path;
    }

    private static bool WalkScope(
        int? parentId, int? triggerChoiceIndex,
        List<Question> allQuestions,
        Dictionary<int, string> answersById,
        Dictionary<int, QuestionTranslation> translationByQId,
        Dictionary<int, string> endingValueById,
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

            if (!answersById.TryGetValue(child.QuestionId, out var answer)) continue;

            var selected = SplitSelected(child.QuestionFormat, answer);

            // Check if the selected answer terminates the survey (match by translated value)
            if (child.EndingAnswers.Any(ea =>
                    endingValueById.TryGetValue(ea.EndingAnswerId, out var endVal)
                    && endVal is not null
                    && selected.Contains(endVal)))
                return true;

            // Resolve selected answer strings to choice indices for branching
            translationByQId.TryGetValue(child.QuestionId, out var translation);
            var choices = translation?.AnswerChoices?.ToList() ?? new List<string>();

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
                    allQuestions, answersById, translationByQId, endingValueById, path, visited);
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
