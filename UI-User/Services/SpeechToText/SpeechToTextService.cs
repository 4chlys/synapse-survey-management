using SynapseEnqueteManagement.UI.User.Models.SpeechToText;
using SynapseEnqueteManagement.UI.User.Services.Session;

namespace SynapseEnqueteManagement.UI.User.Services.SpeechToText;

/// <summary>
/// Thin proxy between UI-User controllers and the backend API speech-to-text endpoint.
/// Follows the same pattern as <see cref="Ideation.IdeaSupportService"/>.
/// </summary>
public sealed class SpeechToTextService(HttpClient httpClient, ISessionService sessionService)
{
    private const string TokenHeader = "X-Participant-Token";

    /// <summary>Sends audio bytes (base64) to the API and returns the transcript.</summary>
    public async Task<SpeechToTextResponse> TranscribeAsync(
        int projectId,
        string audioDataBase64,
        string mimeType,
        string language)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"api/projects/{projectId}/speechtotext/transcribe");

        request.Content = JsonContent.Create(new SpeechToTextRequest
        {
            AudioData = audioDataBase64,
            MimeType  = mimeType,
            Language  = language,
        });

        AddTokenHeader(request, projectId);

        var response = await httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return new SpeechToTextResponse
            {
                IsSuccess = false,
                ErrorCode = "service_unavailable",
            };

        return await response.Content.ReadFromJsonAsync<SpeechToTextResponse>()
               ?? new SpeechToTextResponse { IsSuccess = false, ErrorCode = "unexpected_response" };
    }

    private void AddTokenHeader(HttpRequestMessage request, int projectId)
    {
        var token = sessionService.GetToken(projectId);
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Add(TokenHeader, token);
    }
}
