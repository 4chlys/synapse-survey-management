using SynapseEnqueteManagement.BL.Domain.Platform.Entities;
using SynapseEnqueteManagement.BL.Stats;
using SynapseEnqueteManagement.DAL;

namespace SynapseEnqueteManagement.BL.Tests.Unit;

public class SystemStatsAggregatorTests
{
    private static Project Proj(int id, int platformId, ProjectState state = ProjectState.Active) =>
        new()
        {
            ProjectId = id,
            ProjectPlatformId = platformId,
            ProjectState = state,
            ShortForm = $"p{id}",
        };

    private static ProjectParticipantStats Stats(
        int projectId, int completed, double avg, double median) =>
        new(projectId, completed, completed, avg, median, 0, 0, 0, 0, Array.Empty<double>());

    private readonly SystemStatsAggregator _aggregator = new();

    [Fact]
    public void Aggregate_SysAvg_IsWeightedByCompletedCount()
    {
        var projects = new[] { Proj(1, 1), Proj(2, 1), Proj(3, 1) };
        var participantStats = new[]
        {
            Stats(1, completed: 10, avg: 100, median: 50),
            Stats(2, completed: 20, avg: 200, median: 100),
            Stats(3, completed: 30, avg: 300, median: 150),
        };

        var result = _aggregator.Aggregate(projects, participantStats);

        // (100*10 + 200*20 + 300*30) / 60 = 14000 / 60
        Assert.Equal(60, result.TotalCompleted);
        Assert.Equal(14000d / 60d, result.SysAvg, 10);
    }

    [Fact]
    public void Aggregate_SysMedian_IsSimpleAverageOverProjectMedians()
    {
        var projects = new[] { Proj(1, 1), Proj(2, 1), Proj(3, 1) };
        var participantStats = new[]
        {
            Stats(1, completed: 10, avg: 0, median: 50),
            Stats(2, completed: 20, avg: 0, median: 100),
            Stats(3, completed: 30, avg: 0, median: 150),
        };

        var result = _aggregator.Aggregate(projects, participantStats);

        // (50 + 100 + 150) / 3 = 100 — NOT weighted by CompletedCount
        Assert.Equal(100d, result.SysMedian, 10);
    }

    [Fact]
    public void Aggregate_NoParticipants_ReturnsZeroes()
    {
        var projects = new[] { Proj(1, 1) };
        var participantStats = Array.Empty<ProjectParticipantStats>();

        var result = _aggregator.Aggregate(projects, participantStats);

        Assert.Equal(0, result.TotalCompleted);
        Assert.Equal(0d, result.SysAvg);
        Assert.Equal(0d, result.SysMedian);
    }

    [Fact]
    public void Aggregate_PlatformRollup_WeightsAvgAndAveragesMedian()
    {
        var projects = new[]
        {
            Proj(1, platformId: 1),
            Proj(2, platformId: 1),
            Proj(3, platformId: 2),
        };
        var participantStats = new[]
        {
            Stats(1, completed: 10, avg: 100, median: 50),
            Stats(2, completed: 20, avg: 200, median: 100),
            Stats(3, completed: 40, avg: 400, median: 200),
        };

        var result = _aggregator.Aggregate(projects, participantStats);

        var platform1 = result.PlatformRows.Single(r => r.PlatformId == 1);
        Assert.Equal(30, platform1.ParticipantCount);
        // (100*10 + 200*20) / 30 = 5000/30
        Assert.Equal(5000d / 30d, platform1.AvgCompletionSeconds, 10);
        // (50 + 100) / 2 = 75
        Assert.Equal(75d, platform1.MedianCompletionSeconds, 10);

        var platform2 = result.PlatformRows.Single(r => r.PlatformId == 2);
        Assert.Equal(40, platform2.ParticipantCount);
        Assert.Equal(400d, platform2.AvgCompletionSeconds, 10);
        Assert.Equal(200d, platform2.MedianCompletionSeconds, 10);
    }

    [Fact]
    public void Aggregate_PlatformRollup_NoStatsForGroup_YieldsZeroAvgAndMedian()
    {
        // Project exists but no participant stats row matches → gStats is empty
        var projects = new[] { Proj(1, platformId: 1) };
        var participantStats = Array.Empty<ProjectParticipantStats>();

        var result = _aggregator.Aggregate(projects, participantStats);

        var platform = result.PlatformRows.Single();
        Assert.Equal(0, platform.ParticipantCount);
        Assert.Equal(0d, platform.AvgCompletionSeconds);
        Assert.Equal(0d, platform.MedianCompletionSeconds);
    }

    [Fact]
    public void Aggregate_PlatformRollup_CountsProjectStatesPerPlatform()
    {
        var projects = new[]
        {
            Proj(1, platformId: 1, state: ProjectState.Active),
            Proj(2, platformId: 1, state: ProjectState.Draft),
            Proj(3, platformId: 1, state: ProjectState.Archived),
            Proj(4, platformId: 1, state: ProjectState.Active),
        };

        var result = _aggregator.Aggregate(projects, []);

        var platform = result.PlatformRows.Single();
        Assert.Equal(2, platform.ActiveCount);
        Assert.Equal(1, platform.DraftCount);
        Assert.Equal(1, platform.ArchivedCount);
    }

    [Fact]
    public void Aggregate_ProjectName_PicksDefaultLanguageTranslationFirst()
    {
        var nl = new Language { Code = "nl" };
        var en = new Language { Code = "en" };
        var project = Proj(1, platformId: 1);
        project.DefaultLanguage = nl;
        project.Translations =
        [
            new ProjectTranslation { Language = en, Name = "English" },
            new ProjectTranslation { Language = nl, Name = "Nederlands" },
        ];

        var result = _aggregator.Aggregate([project], []);

        Assert.Equal("Nederlands", result.ProjectRows[0].ProjectName);
    }

    [Fact]
    public void Aggregate_ProjectName_FallsBackToFirstTranslationWhenDefaultLanguageMissing()
    {
        var en = new Language { Code = "en" };
        var project = Proj(1, platformId: 1);
        // DefaultLanguage is null — only an English translation exists. Picker must still find it.
        project.Translations =
        [
            new ProjectTranslation { Language = en, Name = "Only English" },
        ];

        var result = _aggregator.Aggregate([project], []);

        Assert.Equal("Only English", result.ProjectRows[0].ProjectName);
    }

    [Fact]
    public void Aggregate_ProjectName_EmptyWhenNoTranslationsAtAll()
    {
        var project = Proj(1, platformId: 1); // Translations not assigned
        project.Translations = [];

        var result = _aggregator.Aggregate([project], []);

        Assert.Equal(string.Empty, result.ProjectRows[0].ProjectName);
    }
}
