using System.Globalization;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.BL.Interfaces;
using SynapseEnqueteManagement.DAL.Interfaces;
using SynapseEnqueteManagement.DAL.Stats;

namespace SynapseEnqueteManagement.BL.Implementation;

public class QuestionStatsManager(
    IResponseRepository responseRepository,
    IQuestionRepository questionRepository) : IQuestionStatsManager
{
    public async Task<SingleChoiceStats> ReadSingleChoiceStatsAsync(int projectId, int questionId)
    {
        await ValidateOwnershipAsync(projectId, questionId);
        var question = await questionRepository.ReadByIdWithTranslationsReadOnlyAsync(questionId);
        var counts = await responseRepository.ReadCountsBySingleChoiceQuestionReadOnlyAsync(questionId);
        var total = await responseRepository.CountResponsesByQuestionAsync(questionId);
        // Order ascending by option index so the chart follows the question's choice order.
        var merged = MergeByChoiceIndex(counts, question).OrderBy(c => c.Index);
        return new SingleChoiceStats(questionId, total, merged.Select(c => c.Count).ToList());
    }

    public async Task<MultipleChoiceStats> ReadMultipleChoiceStatsAsync(int projectId, int questionId)
    {
        await ValidateOwnershipAsync(projectId, questionId);
        var question = await questionRepository.ReadByIdWithTranslationsReadOnlyAsync(questionId);
        var counts = await responseRepository.ReadCountsByMultipleChoiceQuestionReadOnlyAsync(questionId);
        var total = await responseRepository.CountResponsesByQuestionAsync(questionId);
        // Keep the existing most-selected-first ordering for the bar chart.
        var merged = MergeByChoiceIndex(counts, question).OrderByDescending(c => c.Count.Count);
        return new MultipleChoiceStats(questionId, total, merged.Select(c => c.Count).ToList());
    }

    // Choice answers are stored as the raw text in the language the participant used, so the same option
    // appears once per language ("zeer tevreden" vs "very satisfied"). Options are positionally aligned across
    // translations, so the option's index is its language-independent identity: regroup the per-text counts by
    // index, sum them, and relabel using the admin's current UI culture. Texts that match no choice keep their
    // own bucket as a fallback.
    private static IEnumerable<(int Index, OptionCount Count)> MergeByChoiceIndex(
        IReadOnlyList<OptionCount> counts, Question question)
    {
        var textToIndex = BuildTextToIndexMap(question);
        var labels = LabelsForCurrentCulture(question);

        return counts
            // Matched texts group by their shared option index; unmatched texts keep their own bucket
            // (keyed by the raw text) so a fallback option is never silently merged with another.
            .GroupBy(c => textToIndex.TryGetValue(c.Option, out var idx) ? $"i{idx}" : $"t{c.Option}")
            .Select(g =>
            {
                var hasIndex = textToIndex.TryGetValue(g.First().Option, out var index);
                var label = hasIndex && index < labels.Count ? labels[index] : g.First().Option;
                return (Index: hasIndex ? index : int.MaxValue, Count: new OptionCount(label, g.Sum(c => c.Count)));
            });
    }

    private static Dictionary<string, int> BuildTextToIndexMap(Question question)
    {
        var map = new Dictionary<string, int>();
        foreach (var translation in question?.Translations ?? Enumerable.Empty<QuestionTranslation>())
        {
            var index = 0;
            foreach (var choice in translation.AnswerChoices ?? Enumerable.Empty<string>())
            {
                map.TryAdd(choice, index);
                index++;
            }
        }
        return map;
    }

    private static IReadOnlyList<string> LabelsForCurrentCulture(Question question)
    {
        var cultureCode = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        var translation = question?.Translations.FirstOrDefault(t => t.Language?.Code == cultureCode)
                          ?? question?.Translations.FirstOrDefault();
        return translation?.AnswerChoices?.ToList() ?? new List<string>();
    }

    public async Task<LikertStats> ReadLikertStatsAsync(int projectId, int questionId)
    {
        await ValidateOwnershipAsync(projectId, questionId);
        var distribution = await responseRepository.ReadLikertDistributionReadOnlyAsync(questionId);
        var total = await responseRepository.CountResponsesByQuestionAsync(questionId);
        return new LikertStats(questionId, total, distribution.ToList());
    }

    public async Task<OpenAnswerPage> ReadOpenAnswerPageAsync(int projectId, int questionId, int skip, int take)
    {
        await ValidateOwnershipAsync(projectId, questionId);
        var answers = await responseRepository.ReadOpenAnswersReadOnlyAsync(questionId, skip, take);
        var total = await responseRepository.CountResponsesByQuestionAsync(questionId);
        return new OpenAnswerPage(questionId, total, answers.ToList());
    }

    public async Task<IReadOnlyList<QuestionLookup>> ReadQuestionsByFormatAsync(int projectId, QuestionFormat format)
    {
        var questions = await questionRepository.ReadByProjectIdWithTranslationsReadOnlyAsync(projectId);
        var cultureCode = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

        return questions
            .Where(q => q.QuestionFormat == format)
            .Select(q =>
            {
                var text = q.Translations
                    .FirstOrDefault(t => t.Language.Code == cultureCode)?.Text
                    ?? q.Translations.FirstOrDefault()?.Text
                    ?? string.Empty;
                return new QuestionLookup(q.QuestionId, text);
            })
            .ToList();
    }

    private async Task ValidateOwnershipAsync(int projectId, int questionId)
    {
        if (!await questionRepository.ExistsForProjectAsync(questionId, projectId))
            throw new KeyNotFoundException($"Question {questionId} not found for project {projectId}.");
    }
}
