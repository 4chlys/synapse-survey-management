namespace SynapseEnqueteManagement.UI.Admin.Services.Email;

// Mirror of API/DTOs/Email/EnqueueEmailDto.cs. Kept simple (string Category /
// Status instead of enums) to avoid taking a Domain reference in UI-Admin;
// the API binds them by name.
public record EnqueueEmailRequest(
    string To,
    string[] Bcc,
    string ReplyTo,
    string Subject,
    string Body,
    string Category,
    string LanguageCode,
    int? ProjectPlatformId,
    int? ProjectId,
    int? IdeaId,
    string CreatedByUserId);

public record EmailJobRow(
    int EmailJobId,
    string To,
    string Subject,
    string Category,
    string ProjectShortForm,
    string Status,
    string LanguageCode,
    int AttemptCount,
    int MaxAttempts,
    DateTime CreatedAt,
    DateTime ScheduledAt,
    DateTime? SentAt,
    string LastError);

public record ParticipantEmailWithLanguage(string Email, string LanguageCode);
