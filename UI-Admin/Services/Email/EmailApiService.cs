using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.WebUtilities;

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
        var query = new Dictionary<string, string?> { ["status"] = status, ["take"] = take.ToString() };
        if (projectPlatformId.HasValue) query["projectPlatformId"] = projectPlatformId.Value.ToString();
        var url = QueryHelpers.AddQueryString("api/admin/emails/queue", query);
        var rows = await http.GetFromJsonAsync<List<EmailJobRow>>(url, ct);
        return rows ?? new List<EmailJobRow>();
    }

    // Null when the API reports the job isn't retryable under the caller's scope
    // (404 — another platform's job, or already gone), so the page can redirect
    // instead of surfacing an error.
    public async Task<EmailJobRow?> RetryAsync(int emailJobId, int? projectPlatformId = null, CancellationToken ct = default)
    {
        var url = $"api/admin/emails/queue/{emailJobId}/retry";
        if (projectPlatformId.HasValue)
            url = QueryHelpers.AddQueryString(url, "projectPlatformId", projectPlatformId.Value.ToString());
        var response = await http.PostAsync(url, content: null, ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EmailJobRow>(cancellationToken: ct);
    }

    private record EnqueueResponse(int EmailJobId);
}
