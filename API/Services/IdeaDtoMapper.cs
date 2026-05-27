using SynapseEnqueteManagement.API.DTOs.Ideation;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.API.Services;

public static class IdeaDtoMapper
{
    public static IdeaDto ToDto(Idea i, int? participantId) => new()
    {
        IdeaId = i.IdeaId,
        Description = i.Description,
        ImageUrl = i.ImageUrl,
        CreatedAt = i.CreatedAt,
        CommentsCount = i.Comments.OfType<TextComment>().Count(),
        IsOwnedByCurrentUser = participantId.HasValue && i.Participant?.ParticipantId == participantId.Value,
        IsFlagged = i.Status == ModerationStatus.Flagged,
        LanguageCode = i.Language?.Code,
        Emojis = GroupEmojiReactions(i.Comments.OfType<IconComment>(), participantId)
    };

    public static TextCommentDto ToCommentDto(TextComment c, int? participantId, int? ideaAuthorParticipantId = null) => new()
    {
        CommentId = c.CommentId,
        Text = c.Text,
        CreatedAt = c.CreatedAt,
        IsOwnedByCurrentUser = participantId.HasValue && c.Participant?.ParticipantId == participantId.Value,
        IsPostedByIdeaAuthor = ideaAuthorParticipantId.HasValue && c.Participant?.ParticipantId == ideaAuthorParticipantId.Value,
        IsFlagged = c.ModerationStatus == ModerationStatus.Flagged,
        LanguageCode = c.Language?.Code,
        Emojis = GroupEmojiReactions(c.EmojiReactions.Where(r => r.Emoji != null), participantId)
    };

    private static List<IconCommentDto> GroupEmojiReactions(IEnumerable<IconComment> reactions, int? participantId) =>
        reactions
            .GroupBy(r => r.Emoji.EmojiId)
            .Select(g => new IconCommentDto
            {
                EmojiId = g.Key,
                Symbol = g.First().Emoji.Symbol,
                Count = g.Count(),
                PostedByCurrentUser = participantId.HasValue &&
                                      g.Any(r => r.Participant?.ParticipantId == participantId.Value)
            }).ToList();
}
