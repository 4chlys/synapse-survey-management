using Microsoft.Extensions.Logging;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.BL.Sorting;
using SynapseEnqueteManagement.DAL;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.BL.Implementation;

public class CommentManager(
    IRepository repository,
    ILogger<CommentManager> logger,
    ISortStrategyFactory<TextComment> sortStrategyFactory) : ICommentManager
{
    public async Task<PagedResult<TextComment>> GetOkCommentsByIdeaAsync(int ideaId, int page, int pageSize, string sortKey, int? excludeParticipantId = null)
    {
        var strategy = sortStrategyFactory.Get(sortKey);
        var filterList = new List<System.Linq.Expressions.Expression<Func<TextComment, bool>>>
        {
            c => c.Idea.IdeaId == ideaId,
            c => c.ModerationStatus != ModerationStatus.Flagged && c.ModerationStatus != ModerationStatus.Declined
        };
        if (excludeParticipantId.HasValue)
            filterList.Add(c => c.Participant.ParticipantId != excludeParticipantId.Value);
        var filters = filterList.ToArray();

        var total = await repository.CountAsync<TextComment>(filters);
        var items = await repository.ReadAsync<TextComment>(
            filters: filters,
            includes: [c => c.EmojiReactions, c => c.Participant],
            includeStrings: ["EmojiReactions.Emoji", "EmojiReactions.Participant"],
            orderBy: strategy.DbOrderBy,
            randomize: strategy.Randomize,
            skip: (page - 1) * pageSize,
            take: pageSize
        );

        return new PagedResult<TextComment> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }

    public async Task<IEnumerable<TextComment>> GetOwnCommentsByIdeaAsync(int ideaId, int participantId)
    {
        return await repository.ReadAsync<TextComment>(
            filters:
            [
                c => c.Idea.IdeaId == ideaId,
                c => c.Participant.ParticipantId == participantId,
                c => c.ModerationStatus != ModerationStatus.Declined
            ],
            includes: [c => c.EmojiReactions, c => c.Participant],
            includeStrings: ["EmojiReactions.Emoji", "EmojiReactions.Participant"],
            orderBy: q => q.OrderByDescending(c => c.CreatedAt)
        );
    }

    public async Task<PagedResult<TextComment>> GetCommentsWithOwnFirstAsync(int ideaId, int page, int pageSize, string sortKey, int? participantId)
    {
        var ownComments = participantId.HasValue
            ? (await GetOwnCommentsByIdeaAsync(ideaId, participantId.Value)).ToList()
            : new List<TextComment>();

        var paged = await GetOkCommentsByIdeaAsync(ideaId, page, pageSize, sortKey, excludeParticipantId: participantId);

        return new PagedResult<TextComment>
        {
            Items = ownComments.Concat(paged.Items),
            Page = paged.Page,
            PageSize = paged.PageSize,
            TotalCount = paged.TotalCount
        };
    }

    public async Task<TextComment> AddTextCommentAsync(int ideaId, int projectId, string text, ModerationStatus status, Participant participant)
    {
        var idea = await repository.ReadByIdAsync<Idea>(ideaId, includeStrings: ["Category.Project"]);
        if (idea == null || idea.Category?.Project?.ProjectId != projectId)
        {
            logger.LogWarning("Idea {IdeaId} not found in project {ProjectId}", ideaId, projectId);
            throw new KeyNotFoundException($"Idea {ideaId} not found");
        }

        var comment = new TextComment
        {
            Idea = idea,
            ModerationStatus = status,
            Text = text,
            Participant = participant,
            CreatedAt = DateTime.UtcNow
        };

        return await repository.CreateAsync(comment);
    }

    public async Task<IEnumerable<Emoji>> GetEmojisAsync()
    {
        return await repository.ReadAsync<Emoji>(
            filters: [e => e.IsActive]
        );
    }

    public async Task<IconComment> AddIconCommentAsync(int ideaId, int emojiId, Participant participant)
    {
        var idea = await repository.ReadByIdAsync<Idea>(ideaId);
        if (idea == null)
        {
            logger.LogWarning("Idea {IdeaId} not found", ideaId);
            throw new KeyNotFoundException($"Idea {ideaId} not found");
        }

        var emoji = await repository.ReadByIdAsync<Emoji>(emojiId);
        if (emoji == null)
        {
            logger.LogWarning("Emoji {EmojiId} not found", emojiId);
            throw new KeyNotFoundException($"Emoji {emojiId} not found");
        }

        var iconComment = new IconComment
        {
            Idea = idea,
            EmojiId = emojiId,
            Participant = participant,
            CreatedAt = DateTime.UtcNow
        };

        return await repository.CreateAsync(iconComment,
            duplicateCheck: ic => ic.Participant.ParticipantId == participant.ParticipantId
                                  && ic.EmojiId == emojiId
                                  && ic.Idea.IdeaId == ideaId
                                  && ic.TextComment == null);
    }

    public async Task<bool> RemoveIconCommentAsync(int ideaId, int emojiId, Participant participant)
    {
        return await repository.DeleteAsync<IconComment>(ic => ic.Participant.ParticipantId == participant.ParticipantId
                                                               && ic.EmojiId == emojiId
                                                               && ic.Idea.IdeaId == ideaId
                                                               && ic.TextComment == null);
    }

    public async Task<IconComment> AddCommentReactionAsync(int commentId, int emojiId, Participant participant)
    {
        var comment = await repository.ReadByIdAsync<TextComment>(commentId);
        if (comment == null)
        {
            logger.LogWarning("TextComment {CommentId} not found", commentId);
            throw new KeyNotFoundException($"TextComment {commentId} not found");
        }

        var emoji = await repository.ReadByIdAsync<Emoji>(emojiId);
        if (emoji == null)
        {
            logger.LogWarning("Emoji {EmojiId} not found", emojiId);
            throw new KeyNotFoundException($"Emoji {emojiId} not found");
        }

        var reaction = new IconComment
        {
            Idea = comment.Idea,
            EmojiId = emojiId,
            TextComment = comment,
            Participant = participant,
            CreatedAt = DateTime.UtcNow
        };

        return await repository.CreateAsync(reaction,
            duplicateCheck: ic => ic.Participant.ParticipantId == participant.ParticipantId
                                  && ic.EmojiId == emojiId
                                  && ic.TextComment.CommentId == commentId);
    }

    public async Task<bool> RemoveCommentReactionAsync(int commentId, int emojiId, Participant participant)
    {
        return await repository.DeleteAsync<IconComment>(ic => ic.Participant.ParticipantId == participant.ParticipantId
                                                               && ic.EmojiId == emojiId
                                                               && ic.TextComment.CommentId == commentId);
    }
}