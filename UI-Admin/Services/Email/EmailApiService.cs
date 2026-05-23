using System.Net.Http.Json;

namespace SynapseEnqueteManagement.UI.Admin.Services.Email;

public class EmailApiService(HttpClient http)
{
    public async Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("api/admin/emails", new { To = to, Subject = subject, Body = body }, ct);
        response.EnsureSuccessStatusCode();
    }
}
