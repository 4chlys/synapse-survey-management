using SynapseEnqueteManagement.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.DAL.Implementation;

public class CommentRepository(DbContext context) : Repository<Comment>(context), ICommentRepository
{
    public Task<int> CountOkTextCommentsByIdeaAsync(int ideaId, int? excludeParticipantId)
    {
        var query = Context.Set<Comment>().OfType<TextComment>()
            .Where(c => c.Idea.IdeaId == ideaId
                        && c.ModerationStatus != ModerationStatus.Flagged
                        && c.ModerationStatus != ModerationStatus.Declined);

        if (excludeParticipantId.HasValue)
            query = query.Where(c => c.Participant.ParticipantId != excludeParticipantId.Value);

        return query.CountAsync();
    }

    public async Task<IEnumerable<TextComment>> ReadOkTextCommentsByIdeaWithReactionsReadOnlyAsync(
        int ideaId,
        int? excludeParticipantId,
        Func<IQueryable<TextComment>, IOrderedQueryable<TextComment>> orderBy,
        bool randomize,
        int skip,
        int take)
    {
        var query = Context.Set<Comment>().OfType<TextComment>()
            .AsNoTracking()
            .Include(c => c.Participant)
            .Include(c => c.EmojiReactions).ThenInclude(r => r.Emoji)
            .Include(c => c.EmojiReactions).ThenInclude(r => r.Participant)
            .Where(c => c.Idea.IdeaId == ideaId
                        && c.ModerationStatus != ModerationStatus.Flagged
                        && c.ModerationStatus != ModerationStatus.Declined);

        if (excludeParticipantId.HasValue)
            query = query.Where(c => c.Participant.ParticipantId != excludeParticipantId.Value);

        IQueryable<TextComment> ordered = randomize
            ? query.OrderBy(_ => EF.Functions.Random())
            : orderBy(query);

        return await ordered.Skip(skip).Take(take).ToListAsync();
    }

    public async Task<IEnumerable<TextComment>> ReadOwnTextCommentsByIdeaWithReactionsReadOnlyAsync(int ideaId, int participantId)
        => await Context.Set<Comment>().OfType<TextComment>()
            .AsNoTracking()
            .Include(c => c.Participant)
            .Include(c => c.EmojiReactions).ThenInclude(r => r.Emoji)
            .Include(c => c.EmojiReactions).ThenInclude(r => r.Participant)
            .Where(c => c.Idea.IdeaId == ideaId
                        && c.Participant.ParticipantId == participantId
                        && c.ModerationStatus != ModerationStatus.Declined)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

    public Task<TextComment> ReadTextCommentByIdWithProjectAndEmojisForUpdateAsync(int commentId)
        => Context.Set<Comment>().OfType<TextComment>()
            .Include(c => c.Idea).ThenInclude(i => i.Category).ThenInclude(ca => ca.Project).ThenInclude(p => p.Emojis)
            .FirstOrDefaultAsync(c => c.CommentId == commentId);

    public async Task<IconComment> CreateIconCommentOnIdeaIfNotDuplicateAsync(IconComment iconComment)
    {
        var exists = await Context.Set<Comment>().OfType<IconComment>()
            .AnyAsync(ic => ic.Participant.ParticipantId == iconComment.Participant.ParticipantId
                            && ic.EmojiId == iconComment.EmojiId
                            && ic.Idea.IdeaId == iconComment.Idea.IdeaId
                            && ic.TextComment == null);
        if (exists) return null;
        await Context.Set<Comment>().AddAsync(iconComment);
        return iconComment;
    }

    public async Task<IconComment> CreateIconCommentOnTextCommentIfNotDuplicateAsync(IconComment iconComment)
    {
        var exists = await Context.Set<Comment>().OfType<IconComment>()
            .AnyAsync(ic => ic.Participant.ParticipantId == iconComment.Participant.ParticipantId
                            && ic.EmojiId == iconComment.EmojiId
                            && ic.TextComment.CommentId == iconComment.TextComment.CommentId);
        if (exists) return null;
        await Context.Set<Comment>().AddAsync(iconComment);
        return iconComment;
    }

    public async Task<bool> DeleteIconCommentOnIdeaAsync(int ideaId, int emojiId, int participantId)
    {
        var entity = await Context.Set<Comment>().OfType<IconComment>()
            .FirstOrDefaultAsync(ic => ic.Participant.ParticipantId == participantId
                                       && ic.EmojiId == emojiId
                                       && ic.Idea.IdeaId == ideaId
                                       && ic.TextComment == null);
        if (entity is null) return false;
        Context.Set<Comment>().Remove(entity);
        return true;
    }

    public async Task<bool> DeleteIconCommentOnTextCommentAsync(int commentId, int emojiId, int participantId)
    {
        var entity = await Context.Set<Comment>().OfType<IconComment>()
            .FirstOrDefaultAsync(ic => ic.Participant.ParticipantId == participantId
                                       && ic.EmojiId == emojiId
                                       && ic.TextComment.CommentId == commentId);
        if (entity is null) return false;
        Context.Set<Comment>().Remove(entity);
        return true;
    }
}
