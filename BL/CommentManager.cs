using Microsoft.Extensions.Logging;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.BL.Sorting;
using SynapseEnqueteManagement.DAL;

namespace SynapseEnqueteManagement.BL;

public class CommentManager(IRepository repository, ILogger<CommentManager> logger, ISortStrategyFactory<TextComment> sortStrategyFactory) : ICommentManager
{
    public async Task<PagedResult<TextComment>> GetOkCommentsByIdeaAsync(int ideaId, int page, int pageSize, string sortKey)
    {
        var strategy = sortStrategyFactory.Get(sortKey);
        System.Linq.Expressions.Expression<Func<TextComment, bool>>[] filters =
        [
            c => c.Idea.IdeaId == ideaId,
            c => c.ModerationStatus != ModerationStatus.Flagged && c.ModerationStatus != ModerationStatus.Declined
        ];

        var total = await repository.CountAsync<TextComment>(filters);
        var items = await repository.ReadAsync<TextComment>(
            filters: filters,
            includes: [c => c.EmojiReactions],
            includeStrings: ["EmojiReactions.Emoji", "EmojiReactions.Participant"],
            orderBy: strategy.DbOrderBy,
            randomize: strategy.Randomize,
            skip: (page - 1) * pageSize,
            take: pageSize
        );

        return new PagedResult<TextComment> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
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
        return await repository.DeleteAsync<IconComment>(
            ic => ic.Participant.ParticipantId == participant.ParticipantId
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
        return await repository.DeleteAsync<IconComment>(
            ic => ic.Participant.ParticipantId == participant.ParticipantId
                  && ic.EmojiId == emojiId
                  && ic.TextComment.CommentId == commentId);
    }
}