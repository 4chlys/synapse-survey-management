using Microsoft.Extensions.Logging;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.BL.Helpers;
using SynapseEnqueteManagement.BL.Sorting;
using SynapseEnqueteManagement.DAL.Interfaces;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.BL.Implementation;

public class CommentManager(
    ICommentRepository commentRepository,
    IIdeaRepository ideaRepository,
    IUnitOfWork uow,
    ILogger<CommentManager> logger,
    ISortStrategyFactory<TextComment> sortStrategyFactory) : ICommentManager
{
    public async Task<PagedResult<TextComment>> GetCommentsWithOwnFirstReadOnlyAsync(int projectId, int ideaId, int page, string sortKey, int pageSize, int? participantId)
    {
        var ownComments = participantId.HasValue
            ? await commentRepository.ReadOwnTextCommentsByIdeaWithReactionsReadOnlyAsync(ideaId, participantId.Value)
            : Enumerable.Empty<TextComment>();

        var strategy = sortStrategyFactory.Get(sortKey);
        var total = await commentRepository.CountOkTextCommentsByIdeaAsync(ideaId, participantId);
        var pagedItems = await commentRepository.ReadOkTextCommentsByIdeaWithReactionsReadOnlyAsync(
            ideaId, participantId, strategy.DbOrderBy, strategy.Randomize, (page - 1) * pageSize, pageSize);

        return new PagedResult<TextComment>
        {
            Items = ownComments.Concat(pagedItems),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<TextComment> AddTextCommentAsync(int ideaId, int projectId, string text, ModerationStatus status, Participant participant, string submissionLanguageCode = null, CancellationToken ct = default)
    {
        var idea = await ideaRepository.ReadByIdWithProjectForUpdateAsync(ideaId);
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
            Language = ContentLanguageResolver.Resolve(participant, idea.Category.Project, submissionLanguageCode),
            CreatedAt = DateTime.UtcNow
        };
        EntityValidator.Validate(comment);
        var created = (TextComment)await commentRepository.CreateAsync(comment);
        await uow.SaveChangesAsync(ct);
        return created;
    }

    public async Task<IconComment> AddIconCommentAsync(int ideaId, int projectId, int emojiId, Participant participant, CancellationToken ct = default)
    {
        var idea = await ideaRepository.ReadByIdWithProjectAndEmojisForUpdateAsync(ideaId);
        if (idea?.Category?.Project?.ProjectId != projectId)
        {
            logger.LogWarning("Idea {IdeaId} not found in project {ProjectId}", ideaId, projectId);
            throw new KeyNotFoundException($"Idea {ideaId} not found in project {projectId}");
        }

        if (idea.Category.Project.Emojis.All(e => e.EmojiId != emojiId))
        {
            logger.LogWarning("Emoji {EmojiId} not enabled for project {ProjectId}", emojiId, projectId);
            throw new KeyNotFoundException($"Emoji {emojiId} not enabled for project {projectId}");
        }

        var iconComment = new IconComment
        {
            Idea = idea,
            Emoji = idea.Category.Project.Emojis.First(e => e.EmojiId == emojiId),
            Participant = participant,
            Language = ContentLanguageResolver.Resolve(participant, idea.Category.Project, null),
            CreatedAt = DateTime.UtcNow
        };
        EntityValidator.Validate(iconComment);

        var created = await commentRepository.CreateIconCommentOnIdeaIfNotDuplicateAsync(iconComment);
        if (created == null) return null;
        await uow.SaveChangesAsync(ct);
        return created;
    }

    public async Task<bool> RemoveIconCommentAsync(int ideaId, int projectId, int emojiId, Participant participant, CancellationToken ct = default)
    {
        var removed = await commentRepository.DeleteIconCommentOnIdeaAsync(ideaId, projectId, emojiId, participant.ParticipantId);
        if (!removed) return false;
        await uow.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IconComment> AddCommentReactionAsync(int commentId, int projectId, int emojiId, Participant participant, CancellationToken ct = default)
    {
        var comment = await commentRepository.ReadTextCommentByIdWithProjectAndEmojisForUpdateAsync(commentId);
        if (comment?.Idea?.Category?.Project?.ProjectId != projectId)
        {
            logger.LogWarning("TextComment {CommentId} not found in project {ProjectId}", commentId, projectId);
            throw new KeyNotFoundException($"TextComment {commentId} not found in project {projectId}");
        }
        var emoji = comment.Idea.Category.Project.Emojis.FirstOrDefault(e => e.EmojiId == emojiId);

        if (emoji == null)
        {
            logger.LogWarning("Emoji {EmojiId} not enabled for project {ProjectId}", emojiId, projectId);
            throw new KeyNotFoundException($"Emoji {emojiId} not enabled for project {projectId}");
        }

        var reaction = new IconComment
        {
            Emoji = emoji,
            Idea = comment.Idea,
            TextComment = comment,
            Participant = participant,
            Language = ContentLanguageResolver.Resolve(participant, comment.Idea.Category.Project, null),
            CreatedAt = DateTime.UtcNow
        };
        EntityValidator.Validate(reaction);

        var created = await commentRepository.CreateIconCommentOnTextCommentIfNotDuplicateAsync(reaction);
        if (created == null) return null;
        await uow.SaveChangesAsync(ct);
        return created;
    }

    public async Task<bool> RemoveCommentReactionAsync(int commentId, int projectId, int emojiId, Participant participant, CancellationToken ct = default)
    {
        var removed = await commentRepository.DeleteIconCommentOnTextCommentAsync(commentId, projectId, emojiId, participant.ParticipantId);
        if (!removed) return false;
        await uow.SaveChangesAsync(ct);
        return true;
    }

    public async Task SetCommentModerationStatusAsync(int commentId, ModerationStatus status, int projectId, CancellationToken ct = default)
    {
        var comment = await commentRepository.ReadTextCommentByIdForUpdateAsync(commentId, projectId)
            ?? throw new KeyNotFoundException($"Comment {commentId} not found in project {projectId}.");
        comment.ModerationStatus = status;
        await uow.SaveChangesAsync(ct);
    }
}
