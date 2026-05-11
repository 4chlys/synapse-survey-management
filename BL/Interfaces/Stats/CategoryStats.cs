using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.BL.Interfaces;

public record IdeaFeedItem(
    int IdeaId,
    string Description,
    string CategoryName,
    int CategoryId,
    int ReactionCount,
    DateTime CreatedAt,
    ModerationStatus Status,
    string ParticipantEmail,
    string ImageUrl = null,
    string LanguageCode = null);
public record ModerationQueueResult(int Approved, int Declined, int Flagged, int Unflagged);
