using System.ComponentModel.DataAnnotations;
using SynapseEnqueteManagement.BL.Domain.Platform.Enums;
// Project + Idea are referenced by their fully-qualified names below: this
// file lives in Platform.Entities, so the bare name "Project" collides with
// the sibling SynapseEnqueteManagement.BL.Domain.Project namespace.
using ProjectEntity = SynapseEnqueteManagement.BL.Domain.Project.Entities.Project;
using IdeaEntity    = SynapseEnqueteManagement.BL.Domain.Project.Entities.Idea;

namespace SynapseEnqueteManagement.BL.Domain.Platform.Entities;

// Queue row for one outbound email. One row == one Resend send.
// Broadcasts to many participants are split into multiple rows server-side
// (≤49 BCC per row, grouped by language) before enqueuing.
//
// Navigation-only foreign keys (Participant-style); EF generates shadow FK
// columns. All three are nullable because different Categories carry
// different context: PasswordReset has no Project/Platform, AdminInvite has
// a Platform but no Project, AdminReply has all three.
public class EmailJob : IHasCreatedAt
{
    public int EmailJobId { get; set; }
    public DateTime CreatedAt { get; set; }

    [Required, MaxLength(200), EmailAddress]
    public string ToAddress { get; set; }

    // Comma-joined BCC list. Single column rather than a child table because
    // a job's recipients are immutable once enqueued and we never query by
    // individual recipient.
    public string BccAddressesCsv { get; set; }

    [MaxLength(200), EmailAddress]
    public string ReplyTo { get; set; }

    [Required, MaxLength(200)]
    public string Subject { get; set; }

    // Body includes the localized "Antwoorden aub aan ..." footer appended by
    // the manager at enqueue time, so the canonical wording is server-owned
    // and an admin can't accidentally delete it from the modal.
    [Required, MaxLength(8000)]
    public string Body { get; set; }

    public EmailJobCategory Category { get; set; }
    public EmailJobStatus Status { get; set; } = EmailJobStatus.Pending;

    // Null for non-localized categories (AdminInvite, PasswordReset).
    [MaxLength(10)]
    public string LanguageCode { get; set; }

    public int AttemptCount { get; set; }
    public int MaxAttempts { get; set; } = 5;

    // Due-at; the worker only picks up rows with ScheduledAt <= now.
    // Bumped by MarkFailed to schedule the next retry attempt.
    public DateTime ScheduledAt { get; set; }
    public string LastError { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? SentAt { get; set; }

    public ProjectPlatform ProjectPlatform { get; set; }
    public ProjectEntity Project { get; set; }
    public IdeaEntity Idea { get; set; }

    // ApplicationUser.Id is an Identity string and lives outside this Domain
    // assembly, so stored as a raw string (no nav property).
    public string CreatedByUserId { get; set; }

    // Optimistic concurrency token; mapped to xmin on Postgres and a
    // RowVersion timestamp on SQLite by the DbContext, matching IdeaSummaryJob.
    public byte[] RowVersion { get; set; }
}
