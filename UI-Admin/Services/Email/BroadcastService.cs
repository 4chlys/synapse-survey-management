using System.Net.Http.Json;

namespace SynapseEnqueteManagement.UI.Admin.Services.Email;

// Splits an admin "Send to all participants" broadcast into N queue rows:
// participants are grouped by language; each group is chunked into ≤49 BCC
// slices (Resend's per-message recipient cap is ~50, one slot taken by To).
public class BroadcastService(HttpClient http, EmailApiService emailApi)
{
    private const int BccChunkSize = 49;

    // perLanguageMessages: key = language code (matching project SupportedLanguages),
    // value = (subject, body). For a Dutch-only survey this dict has one entry.
    // Participants whose Language doesn't appear in the dict fall back to
    // defaultLanguageCode.
    public async Task<BroadcastResult> SendBroadcastAsync(
        int projectId,
        int projectPlatformId,
        IReadOnlyDictionary<string, (string Subject, string Body)> perLanguageMessages,
        string defaultLanguageCode,
        string to,
        string replyTo,
        string createdByUserId,
        CancellationToken ct = default)
    {
        var participants = await http.GetFromJsonAsync<List<ParticipantEmailWithLanguage>>(
            $"api/admin/projects/{projectId}/participants/emails-by-language", ct)
            ?? new List<ParticipantEmailWithLanguage>();

        var byLanguage = participants
            .GroupBy(p => ResolveLanguage(p.LanguageCode, perLanguageMessages, defaultLanguageCode))
            // Drop participants for whom no translation is available at all
            // (would otherwise silently send them nothing).
            .Where(g => g.Key != null);

        var enqueued = 0;
        var recipients = 0;

        foreach (var group in byLanguage)
        {
            var (subject, body) = perLanguageMessages[group.Key];
            var emails = group.Select(p => p.Email).ToList();
            recipients += emails.Count;

            for (var i = 0; i < emails.Count; i += BccChunkSize)
            {
                var chunk = emails.Skip(i).Take(BccChunkSize).ToArray();
                await emailApi.EnqueueAsync(new EnqueueEmailRequest(
                    To:                to,
                    Bcc:               chunk,
                    ReplyTo:           replyTo,
                    Subject:           subject,
                    Body:              body,
                    Category:          "AdminBroadcast",
                    LanguageCode:      group.Key,
                    ProjectPlatformId: projectPlatformId,
                    ProjectId:         projectId,
                    IdeaId:            null,
                    CreatedByUserId:   createdByUserId), ct);
                enqueued++;
            }
        }

        return new BroadcastResult(enqueued, recipients);
    }

    private static string ResolveLanguage(
        string participantLanguage,
        IReadOnlyDictionary<string, (string, string)> available,
        string defaultLanguageCode)
    {
        if (!string.IsNullOrEmpty(participantLanguage) && available.ContainsKey(participantLanguage))
            return participantLanguage;
        if (!string.IsNullOrEmpty(defaultLanguageCode) && available.ContainsKey(defaultLanguageCode))
            return defaultLanguageCode;
        return available.Keys.FirstOrDefault();
    }
}

public record BroadcastResult(int JobsEnqueued, int RecipientsReached);
