using Microsoft.EntityFrameworkCore;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.DAL.Interfaces;
using SynapseEnqueteManagement.DAL.Stats;

namespace SynapseEnqueteManagement.DAL.Implementation;

public class QuestionStatsRepository(DbContext context) : IQuestionStatsRepository
{
    public async Task<IReadOnlyList<OptionCount>> ReadCountsBySingleChoiceQuestionReadOnlyAsync(int questionId)
    {
        var rows = await context.Set<Response>()
            .AsNoTracking()
            .Where(r => EF.Property<int>(r, "QuestionId") == questionId && r.IsCommitted)
            .GroupBy(r => r.Value)
            .Select(g => new { Option = g.Key, Count = g.Count() })
            .ToListAsync();
        return rows.Select(x => new OptionCount(x.Option, x.Count)).ToList();
    }

    // MultipleChoice values are ", "-joined texts; SQL cannot split strings portably across SQLite and PostgreSQL,
    // so the split and grouping happen in C# after a single round-trip that pushes the WHERE to SQL.
    public async Task<IReadOnlyList<OptionCount>> ReadCountsByMultipleChoiceQuestionReadOnlyAsync(int questionId)
    {
        var values = await context.Set<Response>()
            .AsNoTracking()
            .Where(r => EF.Property<int>(r, "QuestionId") == questionId && r.IsCommitted)
            .Select(r => r.Value)
            .ToListAsync();
        return values
            .Where(v => !string.IsNullOrEmpty(v))
            .SelectMany(v => v.Split(", ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            .GroupBy(choice => choice)
            .Select(g => new OptionCount(g.Key, g.Count()))
            .OrderByDescending(x => x.Count)
            .ToList();
    }

    // Likert values are numeric strings; GROUP BY in SQL, then parse to int for numeric ordering.
    public async Task<IReadOnlyList<LikertPointCount>> ReadLikertDistributionReadOnlyAsync(int questionId)
    {
        var rows = await context.Set<Response>()
            .AsNoTracking()
            .Where(r => EF.Property<int>(r, "QuestionId") == questionId && r.IsCommitted)
            .GroupBy(r => r.Value)
            .Select(g => new { Point = g.Key, Count = g.Count() })
            .ToListAsync();
        return rows
            .Where(x => int.TryParse(x.Point, out _))
            .Select(x => new LikertPointCount(int.Parse(x.Point), x.Count))
            .OrderBy(x => x.Point)
            .ToList();
    }

    public async Task<IReadOnlyList<OpenAnswer>> ReadOpenAnswersReadOnlyAsync(int questionId, int skip, int take)
        => await context.Set<Response>()
            .AsNoTracking()
            .Where(r => EF.Property<int>(r, "QuestionId") == questionId && r.IsCommitted)
            .OrderByDescending(r => r.CreatedAt)
            .Skip(skip)
            .Take(take)
            .Select(r => new OpenAnswer(r.Value, r.CreatedAt))
            .ToListAsync();

    public Task<int> CountResponsesByQuestionAsync(int questionId)
        => context.Set<Response>()
            .CountAsync(r => EF.Property<int>(r, "QuestionId") == questionId && r.IsCommitted);
}
