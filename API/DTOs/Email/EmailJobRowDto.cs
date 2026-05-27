namespace SynapseEnqueteManagement.API.DTOs.Email;

// Shape returned to UI-Admin's operator queue page. Category/Status are
// strings (enum .ToString()) so the default JSON setup round-trips them
// cleanly to UI-Admin, which reads them as strings. Excludes Body to keep
// list responses small — the operator page does not display message bodies.
public record EmailJobRowDto(
    int EmailJobId,
    string To,
    string Subject,
    string Category,
    string Status,
    string LanguageCode,
    int AttemptCount,
    int MaxAttempts,
    DateTime CreatedAt,
    DateTime ScheduledAt,
    DateTime? SentAt,
    string LastError);
