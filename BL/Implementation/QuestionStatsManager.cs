using System.Globalization;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.BL.Interfaces;
using SynapseEnqueteManagement.DAL.Interfaces;

namespace SynapseEnqueteManagement.BL.Implementation;

public class QuestionStatsManager(
    IResponseRepository responseRepository,
    IQuestionRepository questionRepository) : IQuestionStatsManager
{
    public async Task<SingleChoiceStats> ReadSingleChoiceStatsAsync(int projectId, int questionId)
    {
        await ValidateOwnershipAsync(projectId, questionId);
        var counts = await responseRepository.ReadCountsBySingleChoiceQuestionReadOnlyAsync(questionId);
        var total = await responseRepository.CountResponsesByQuestionAsync(questionId);
        return new SingleChoiceStats(questionId, total, counts.ToList());
    }

    public async Task<MultipleChoiceStats> ReadMultipleChoiceStatsAsync(int projectId, int questionId)
    {
        await ValidateOwnershipAsync(projectId, questionId);
        var counts = await responseRepository.ReadCountsByMultipleChoiceQuestionReadOnlyAsync(questionId);
        var total = await responseRepository.CountResponsesByQuestionAsync(questionId);
        return new MultipleChoiceStats(questionId, total, counts.ToList());
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
