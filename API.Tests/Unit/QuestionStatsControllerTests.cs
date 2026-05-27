using SynapseEnqueteManagement.API.DTOs.Stats.QuestionStats;
using SynapseEnqueteManagement.DAL.Stats;

namespace SynapseEnqueteManagement.API.Tests;

public class QuestionStatsControllerTests
{
    private readonly Mock<IQuestionStatsManager> _statsManager = new();
    private readonly Mock<IProjectManager> _projectManager = new();
    private readonly QuestionStatsController _controller;

    private const int ProjectId = 1;
    private const int QuestionId = 42;

    public QuestionStatsControllerTests()
    {
        _controller = new QuestionStatsController(_statsManager.Object, _projectManager.Object);
    }

    #region Auth-required: project must exist

    [Fact]
    public async Task GetQuestions_ProjectNotFound_ReturnsNotFound()
    {
        _projectManager.Setup(m => m.GetProjectByIdReadOnlyAsync(ProjectId))
            .ReturnsAsync((Project)null);

        var result = await _controller.GetQuestions(ProjectId, QuestionFormat.SingleChoice);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    #endregion

    #region GetQuestions

    [Fact]
    public async Task GetQuestions_NoFormat_ReturnsBadRequest()
    {
        var result = await _controller.GetQuestions(ProjectId, null);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetQuestions_ValidRequest_ReturnsLookupDtos()
    {
        _projectManager.Setup(m => m.GetProjectByIdReadOnlyAsync(ProjectId))
            .ReturnsAsync(new Project { ProjectId = ProjectId });
        _statsManager.Setup(m => m.ReadQuestionsByFormatAsync(ProjectId, QuestionFormat.SingleChoice, It.IsAny<string>()))
            .ReturnsAsync(new QuestionLookup[]
            {
                new(1, "Question One"),
                new(2, "Question Two"),
            });

        var result = await _controller.GetQuestions(ProjectId, QuestionFormat.SingleChoice);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dtos = Assert.IsAssignableFrom<ICollection<QuestionLookupDto>>(ok.Value);
        Assert.Equal(2, dtos.Count);
        Assert.Contains(dtos, d => d.QuestionId == 1 && d.Text == "Question One");
    }

    #endregion

    #region GetSingleChoiceStats

    [Fact]
    public async Task GetSingleChoiceStats_ValidRequest_ReturnsMappedDto()
    {
        var counts = new List<OptionCount> { new("Yes", 7), new("No", 3) };
        _projectManager.Setup(m => m.GetProjectByIdReadOnlyAsync(ProjectId))
            .ReturnsAsync(new Project { ProjectId = ProjectId });
        _statsManager.Setup(m => m.ReadSingleChoiceStatsAsync(ProjectId, QuestionId, It.IsAny<string>()))
            .ReturnsAsync(new SingleChoiceStats(QuestionId, 10, counts));

        var result = await _controller.GetSingleChoiceStats(ProjectId, QuestionId);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<SingleChoiceStatsDto>(ok.Value);
        Assert.Equal(QuestionId, dto.QuestionId);
        Assert.Equal(10, dto.TotalResponses);
        Assert.Equal(2, dto.Counts.Count);
        Assert.Contains(dto.Counts, c => c.Option == "Yes" && c.Count == 7);
    }

    #endregion

    #region GetMultipleChoiceStats

    [Fact]
    public async Task GetMultipleChoiceStats_ValidRequest_ReturnsMappedDto()
    {
        var counts = new List<OptionCount> { new("A", 4), new("B", 6), new("C", 1) };
        _projectManager.Setup(m => m.GetProjectByIdReadOnlyAsync(ProjectId))
            .ReturnsAsync(new Project { ProjectId = ProjectId });
        _statsManager.Setup(m => m.ReadMultipleChoiceStatsAsync(ProjectId, QuestionId, It.IsAny<string>()))
            .ReturnsAsync(new MultipleChoiceStats(QuestionId, 11, counts));

        var result = await _controller.GetMultipleChoiceStats(ProjectId, QuestionId);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<MultipleChoiceStatsDto>(ok.Value);
        Assert.Equal(QuestionId, dto.QuestionId);
        Assert.Equal(11, dto.TotalResponses);
        Assert.Equal(3, dto.Counts.Count);
    }

    #endregion

    #region GetLikertStats

    [Fact]
    public async Task GetLikertStats_ValidRequest_ReturnsMappedDto()
    {
        var distribution = new List<LikertPointCount> { new(1, 2), new(3, 6), new(5, 4) };
        _statsManager.Setup(m => m.ReadLikertStatsAsync(ProjectId, QuestionId))
            .ReturnsAsync(new LikertStats(QuestionId, 12, distribution));

        var result = await _controller.GetLikertStats(ProjectId, QuestionId);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<LikertStatsDto>(ok.Value);
        Assert.Equal(QuestionId, dto.QuestionId);
        Assert.Equal(12, dto.TotalResponses);
        Assert.Equal(3, dto.Distribution.Count);
        Assert.Contains(dto.Distribution, d => d.Point == 1 && d.Count == 2);
    }

    #endregion

    #region GetOpenAnswers

    [Fact]
    public async Task GetOpenAnswers_ValidRequest_ReturnsMappedPage()
    {
        var now = DateTime.UtcNow;
        var answers = new List<OpenAnswer> { new("Great idea!", now), new("I agree.", now) };
        _statsManager.Setup(m => m.ReadOpenAnswerPageAsync(ProjectId, QuestionId, 0, 50))
            .ReturnsAsync(new OpenAnswerPage(QuestionId, 2, answers));

        var result = await _controller.GetOpenAnswers(ProjectId, QuestionId);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<OpenAnswerPageDto>(ok.Value);
        Assert.Equal(QuestionId, dto.QuestionId);
        Assert.Equal(2, dto.TotalAnswers);
        Assert.Equal(2, dto.Answers.Count);
        Assert.Contains(dto.Answers, a => a.Value == "Great idea!");
    }

    #endregion
}
