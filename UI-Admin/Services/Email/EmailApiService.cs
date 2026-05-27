using System.Net.Http.Json;

namespace SynapseEnqueteManagement.UI.Admin.Services.Email;

public class EmailApiService(HttpClient http)
{
    // Legacy synchronous endpoint — kept for callers that still want a
    // blocking send. New code should call EnqueueAsync.
    public async Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("api/admin/emails", new { To = to, Subject = subject, Body = body }, ct);
        response.EnsureSuccessStatusCode();
    }

    // Non-blocking: persists one row in the EmailJob queue; the worker sends
    // it within ~5 s. Returns the new job id (for audit/logging).
    public async Task<int> EnqueueAsync(EnqueueEmailRequest req, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("api/admin/emails/queue", req, ct);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<EnqueueResponse>(cancellationToken: ct);
        return payload?.EmailJobId ?? 0;
    }

    public async Task<IReadOnlyList<EmailJobRow>> ListQueueAsync(
        string status, int? projectPlatformId, int take, CancellationToken ct = default)
    {
        var url = $"api/admin/emails/queue?status={Uri.EscapeDataString(status)}&take={take}";
        if (projectPlatformId.HasValue) url += $"&projectPlatformId={projectPlatformId.Value}";
        var rows = await http.GetFromJsonAsync<List<EmailJobRow>>(url, ct);
        return rows ?? new List<EmailJobRow>();
    }

    public async Task<EmailJobRow> RetryAsync(int emailJobId, CancellationToken ct = default)
    {
        var response = await http.PostAsync($"api/admin/emails/queue/{emailJobId}/retry", content: null, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EmailJobRow>(cancellationToken: ct);
    }

    private record EnqueueResponse(int EmailJobId);
}
