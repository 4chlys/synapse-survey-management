using SynapseEnqueteManagement.UI.User.Services.Moderation;
using SynapseEnqueteManagement.UI.User.Services.Session;

namespace SynapseEnqueteManagement.UI.User.Services.Ideation;

// Request / response models (mirror of API DTOs)

public sealed class IdeaSupportMessage
{
    public string Role    { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public sealed class IdeaSupportChatResponse
{
    public string AssistantMessage { get; set; } = string.Empty;
    public string SuggestedDraft   { get; set; }
    public string Phase            { get; set; } = "Ideation";
}

public sealed class IdeaSupportRefineResponse
{
    public string RefinedText { get; set; }
}

// Service

/// <summary>
/// Thin proxy between UI-User controllers and the backend API idea-support
/// endpoints. Follows the same pattern as <see cref="ModerationService"/>.
/// </summary>
public sealed class IdeaSupportService(HttpClient httpClient, ISessionService sessionService)
{
    private const string TokenHeader = "X-Participant-Token";

    /// <summary>
    /// Sends a chat turn to the API and returns the assistant's reply.
    /// </summary>
    public async Task<IdeaSupportChatResponse> ChatAsync(
        int projectId,
        int categoryId,
        IEnumerable<IdeaSupportMessage> history,
        string userMessage)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"api/projects/{projectId}/ideasupport/chat");

        request.Content = JsonContent.Create(new
        {
            History     = history,
            UserMessage = userMessage,
            CategoryId  = categoryId,
        });

        AddTokenHeader(request, projectId);

        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();   // throws HttpRequestException on non-2xx

        return await response.Content.ReadFromJsonAsync<IdeaSupportChatResponse>()
               ?? new IdeaSupportChatResponse { AssistantMessage = string.Empty };
    }

    /// <summary>
    /// Polishes a draft idea without any chat context.
    /// </summary>
    public async Task<IdeaSupportRefineResponse> RefineAsync(int projectId, string draftText)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"api/projects/{projectId}/ideasupport/refine");

        request.Content = JsonContent.Create(new { DraftText = draftText });
        AddTokenHeader(request, projectId);

        var response = await httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return new IdeaSupportRefineResponse { RefinedText = null };

        return await response.Content.ReadFromJsonAsync<IdeaSupportRefineResponse>()
               ?? new IdeaSupportRefineResponse { RefinedText = null };
    }

    private void AddTokenHeader(HttpRequestMessage request, int projectId)
    {
        var token = sessionService.GetToken(projectId);
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Add(TokenHeader, token);
    }
}