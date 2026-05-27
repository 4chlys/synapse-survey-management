using SynapseEnqueteManagement.BL.Domain.Platform.Entities;
using SynapseEnqueteManagement.BL.Domain.Platform.Enums;

namespace SynapseEnqueteManagement.DAL.Interfaces;

public interface IEmailJobRepository
{
    // Foreign-key ids are passed alongside the entity instead of as nav
    // properties on EmailJob (per "ids live in DAL"). The repo plumbs them
    // into EF's shadow FK columns before SaveChanges.
    Task<EmailJob> CreateAsync(
        EmailJob job,
        int? projectPlatformId,
        int? projectId,
        int? ideaId,
        CancellationToken ct = default);

    Task<EmailJob> GetByIdAsync(int emailJobId, CancellationToken ct = default);

    // Returns the next due Pending row (ScheduledAt<=now), already flipped to
    // Processing and saved. Returns null if no row is due or if another
    // worker won the race (DbUpdateConcurrencyException).
    Task<EmailJob> ClaimNextDueAsync(CancellationToken ct = default);

    Task UpdateAsync(EmailJob job, CancellationToken ct = default);

    // Operator-page listing — read-only, capped per call.
    Task<List<EmailJob>> ListByStatusReadOnlyAsync(
        EmailJobStatus status, int? projectPlatformId, int take, CancellationToken ct = default);

    Task<List<EmailJob>> ListRecentSentReadOnlyAsync(
        int? projectPlatformId, int take, CancellationToken ct = default);
}
