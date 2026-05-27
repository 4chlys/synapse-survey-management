using SynapseEnqueteManagement.BL.Domain.Platform.Enums;

namespace SynapseEnqueteManagement.API.DTOs.Email;

// Shape returned to UI-Admin's operator queue page. Excludes Body to keep
// list responses small — the operator page does not display message bodies.
public record EmailJobRowDto(
    int EmailJobId,
    string To,
    string Subject,
    EmailJobCategory Category,
    EmailJobStatus Status,
    string LanguageCode,
    int AttemptCount,
    int MaxAttempts,
    DateTime CreatedAt,
    DateTime ScheduledAt,
    DateTime? SentAt,
    string LastError);
