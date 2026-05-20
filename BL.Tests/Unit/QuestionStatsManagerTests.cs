using SynapseEnqueteManagement.DAL.Stats;

namespace SynapseEnqueteManagement.BL.Tests.Unit;

public class QuestionStatsManagerTests
{
    private readonly Mock<IResponseRepository> _responseRepo = new(MockBehavior.Strict);
    private readonly Mock<IQuestionRepository> _questionRepo = new(MockBehavior.Strict);
    private readonly QuestionStatsManager _manager;

    private const int ProjectId = 1;
    private const int QuestionId = 42;

    public QuestionStatsManagerTests()
    {
        _manager = new QuestionStatsManager(_responseRepo.Object, _questionRepo.Object);
    }

    private void SetupOwnership(bool exists)
    {
        _questionRepo.Setup(r => r.ExistsForProjectAsync(QuestionId, ProjectId))
            .ReturnsAsync(exists);
    }

    #region Project-ownership validation

    [Fact]
    public async Task ReadSingleChoiceStatsAsync_QuestionNotInProject_ThrowsKeyNotFoundException()
    {
        SetupOwnership(false);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _manager.ReadSingleChoiceStatsAsync(ProjectId, QuestionId));
    }

    [Fact]
    public async Task ReadMultipleChoiceStatsAsync_QuestionNotInProject_ThrowsKeyNotFoundException()
    {
        SetupOwnership(false);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _manager.ReadMultipleChoiceStatsAsync(ProjectId, QuestionId));
    }

    [Fact]
    public async Task ReadLikertStatsAsync_QuestionNotInProject_ThrowsKeyNotFoundException()
    {
        SetupOwnership(false);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _manager.ReadLikertStatsAsync(ProjectId, QuestionId));
    }

    [Fact]
    public async Task ReadOpenAnswerPageAsync_QuestionNotInProject_ThrowsKeyNotFoundException()
    {
        SetupOwnership(false);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _manager.ReadOpenAnswerPageAsync(ProjectId, QuestionId, 0, 50));
    }

    #endregion

    #region Correct stats record per format

    [Fact]
    public async Task ReadSingleChoiceStatsAsync_ValidQuestion_ReturnsCorrectStats()
    {
        SetupOwnership(true);
        var counts = new OptionCount[] { new("Yes", 7), new("No", 3) };
        _responseRepo.Setup(r => r.ReadCountsBySingleChoiceQuestionReadOnlyAsync(QuestionId))
            .ReturnsAsync(counts);
        _responseRepo.Setup(r => r.CountResponsesByQuestionAsync(QuestionId))
            .ReturnsAsync(10);

        var result = await _manager.ReadSingleChoiceStatsAsync(ProjectId, QuestionId);

        Assert.Equal(QuestionId, result.QuestionId);
        Assert.Equal(10, result.TotalResponses);
        Assert.Equal(2, result.Counts.Count);
        Assert.Contains(result.Counts, c => c.Option == "Yes" && c.Count == 7);
        Assert.Contains(result.Counts, c => c.Option == "No" && c.Count == 3);
    }

    [Fact]
    public async Task ReadMultipleChoiceStatsAsync_ValidQuestion_ReturnsCorrectStats()
    {
        SetupOwnership(true);
        var counts = new OptionCount[] { new("A", 4), new("B", 6), new("C", 1) };
        _responseRepo.Setup(r => r.ReadCountsByMultipleChoiceQuestionReadOnlyAsync(QuestionId))
            .ReturnsAsync(counts);
        _responseRepo.Setup(r => r.CountResponsesByQuestionAsync(QuestionId))
            .ReturnsAsync(11);

        var result = await _manager.ReadMultipleChoiceStatsAsync(ProjectId, QuestionId);

        Assert.Equal(QuestionId, result.QuestionId);
        Assert.Equal(11, result.TotalResponses);
        Assert.Equal(3, result.Counts.Count);
        Assert.Contains(result.Counts, c => c.Option == "A" && c.Count == 4);
    }

    [Fact]
    public async Task ReadLikertStatsAsync_ValidQuestion_ReturnsCorrectDistribution()
    {
        SetupOwnership(true);
        var distribution = new LikertPointCount[] { new(1, 2), new(3, 5), new(5, 3) };
        _responseRepo.Setup(r => r.ReadLikertDistributionReadOnlyAsync(QuestionId))
            .ReturnsAsync(distribution);
        _responseRepo.Setup(r => r.CountResponsesByQuestionAsync(QuestionId))
            .ReturnsAsync(10);

        var result = await _manager.ReadLikertStatsAsync(ProjectId, QuestionId);

        Assert.Equal(QuestionId, result.QuestionId);
        Assert.Equal(10, result.TotalResponses);
        Assert.Equal(3, result.Distribution.Count);
        Assert.Contains(result.Distribution, d => d.Point == 1 && d.Count == 2);
        Assert.Contains(result.Distribution, d => d.Point == 5 && d.Count == 3);
    }

    [Fact]
    public async Task ReadOpenAnswerPageAsync_ValidQuestion_ReturnsAnswersAndTotal()
    {
        SetupOwnership(true);
        var now = DateTime.UtcNow;
        var answers = new OpenAnswer[] { new("Hello world", now), new("Another answer", now) };
        _responseRepo.Setup(r => r.ReadOpenAnswersReadOnlyAsync(QuestionId, 0, 50))
            .ReturnsAsync(answers);
        _responseRepo.Setup(r => r.CountResponsesByQuestionAsync(QuestionId))
            .ReturnsAsync(2);

        var result = await _manager.ReadOpenAnswerPageAsync(ProjectId, QuestionId, 0, 50);

        Assert.Equal(QuestionId, result.QuestionId);
        Assert.Equal(2, result.TotalAnswers);
        Assert.Equal(2, result.Answers.Count);
        Assert.Contains(result.Answers, a => a.Value == "Hello world");
    }

    #endregion

    #region Open-answer paging

    [Fact]
    public async Task ReadOpenAnswerPageAsync_PassesSkipAndTakeThroughToRepository()
    {
        SetupOwnership(true);
        int capturedSkip = -1, capturedTake = -1;
        _responseRepo
            .Setup(r => r.ReadOpenAnswersReadOnlyAsync(QuestionId, It.IsAny<int>(), It.IsAny<int>()))
            .Callback<int, int, int>((_, skip, take) => { capturedSkip = skip; capturedTake = take; })
            .ReturnsAsync(Array.Empty<OpenAnswer>());
        _responseRepo.Setup(r => r.CountResponsesByQuestionAsync(QuestionId))
            .ReturnsAsync(0);

        await _manager.ReadOpenAnswerPageAsync(ProjectId, QuestionId, 25, 10);

        Assert.Equal(25, capturedSkip);
        Assert.Equal(10, capturedTake);
    }

    #endregion

    #region ReadQuestionsByFormatAsync

    [Fact]
    public async Task ReadQuestionsByFormatAsync_ReturnsOnlyQuestionsMatchingFormat()
    {
        var questions = new Question[]
        {
            new() { QuestionId = 1, QuestionFormat = QuestionFormat.SingleChoice },
            new() { QuestionId = 2, QuestionFormat = QuestionFormat.Open },
            new() { QuestionId = 3, QuestionFormat = QuestionFormat.SingleChoice },
            new() { QuestionId = 4, QuestionFormat = QuestionFormat.Likert },
        };
        _questionRepo.Setup(r => r.ReadByProjectIdWithTranslationsReadOnlyAsync(ProjectId))
            .ReturnsAsync(questions);

        var result = await _manager.ReadQuestionsByFormatAsync(ProjectId, QuestionFormat.SingleChoice);

        Assert.Equal(2, result.Count);
        Assert.All(result, q => Assert.Contains(q.QuestionId, new[] { 1, 3 }));
    }

    [Fact]
    public async Task ReadQuestionsByFormatAsync_NoMatchingFormat_ReturnsEmptyList()
    {
        _questionRepo.Setup(r => r.ReadByProjectIdWithTranslationsReadOnlyAsync(ProjectId))
            .ReturnsAsync(Array.Empty<Question>());

        var result = await _manager.ReadQuestionsByFormatAsync(ProjectId, QuestionFormat.MultipleChoice);

        Assert.Empty(result);
    }

    #endregion
}
