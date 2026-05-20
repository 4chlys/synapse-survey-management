using SynapseEnqueteManagement.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.DAL.Stats;

namespace SynapseEnqueteManagement.DAL.Implementation;

public class ResponseRepository(DbContext context) : Repository<Response>(context), IResponseRepository
{
    public Task<Response> ReadByQuestionAndParticipantReadOnlyAsync(int questionId, int participantId)
        => Context.Set<Response>()
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Question.QuestionId == questionId
                                       && r.Participant.ParticipantId == participantId);

    public Task<Response> ReadByQuestionAndParticipantForUpdateAsync(int questionId, int participantId)
        => Context.Set<Response>()
            .SingleOrDefaultAsync(r => r.Question.QuestionId == questionId
                                       && r.Participant.ParticipantId == participantId);

    public async Task<IEnumerable<Response>> ReadByParticipantWithQuestionForUpdateAsync(int participantId)
        => await Context.Set<Response>()
            .Include(r => r.Question)
            .Where(r => r.Participant.ParticipantId == participantId)
            .ToListAsync();

    public async Task<IEnumerable<Response>> ReadByParticipantWithQuestionTranslationsForUpdateAsync(int participantId)
        => await Context.Set<Response>()
            .Include(r => r.Language)
            .Include(r => r.Question).ThenInclude(q => q.Translations).ThenInclude(t => t.Language)
            .Where(r => r.Participant.ParticipantId == participantId)
            .ToListAsync();

    public async Task<IEnumerable<Response>> ReadByParticipantWithQuestionReadOnlyAsync(int participantId)
        => await Context.Set<Response>()
            .AsNoTracking()
            .Include(r => r.Question)
            .Include(r => r.Language)
            .Where(r => r.Participant.ParticipantId == participantId)
            .ToListAsync();

    // Issues a single DELETE outside the change tracker; callers must wrap in IUnitOfWork.ExecuteInTransactionAsync to preserve rollback.
    public async Task DeleteAllByParticipantIdAsync(int participantId)
    {
        await Context.Set<Response>()
            .Where(r => r.Participant.ParticipantId == participantId)
            .ExecuteDeleteAsync();
    }

    public async Task<IEnumerable<Response>> ReadCommittedByProjectWithQuestionReadOnlyAsync(int projectId)
        => await Context.Set<Response>()
            .AsNoTracking()
            .Include(r => r.Question)
            .Include(r => r.Participant)
            .Where(r => r.IsCommitted && r.Question.Questionnaire.Project.ProjectId == projectId)
            .OrderBy(r => r.Question.Position)
            .ThenBy(r => r.ResponseId)
            .ToListAsync();

    public async Task<IReadOnlyList<OptionCount>> ReadCountsBySingleChoiceQuestionReadOnlyAsync(int questionId)
    {
        var rows = await Context.Set<Response>()
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
        var values = await Context.Set<Response>()
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
        var rows = await Context.Set<Response>()
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
        => await Context.Set<Response>()
            .AsNoTracking()
            .Where(r => EF.Property<int>(r, "QuestionId") == questionId && r.IsCommitted)
            .OrderByDescending(r => r.CreatedAt)
            .Skip(skip)
            .Take(take)
            .Select(r => new OpenAnswer(r.Value, r.CreatedAt))
            .ToListAsync();

    public Task<int> CountResponsesByQuestionAsync(int questionId)
        => Context.Set<Response>()
            .CountAsync(r => EF.Property<int>(r, "QuestionId") == questionId && r.IsCommitted);
}
