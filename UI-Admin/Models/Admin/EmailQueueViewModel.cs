using SynapseEnqueteManagement.UI.Admin.Services.Email;

namespace SynapseEnqueteManagement.UI.Admin.Models.Admin;

// What /Admin/EmailQueue renders. Three buckets:
//   - Pending: rows due now / waiting in backoff (Status=Pending)
//   - GivenUp: rows that hit MaxAttempts (Status=Failed, needs human action)
//   - RecentSent: last N successful sends, for spot-checking
public record EmailQueueViewModel(
    IReadOnlyList<EmailJobRow> Pending,
    IReadOnlyList<EmailJobRow> GivenUp,
    IReadOnlyList<EmailJobRow> RecentSent);
