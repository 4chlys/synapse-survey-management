using SynapseEnqueteManagement.UI.Admin.Models.Project;

namespace SynapseEnqueteManagement.UI.Admin.Services.Project;

public class IdeaAdminService(HttpClient httpClient)
{
    private record IdeaPageResponse
    {
        public int TotalCount { get; init; }
        public int Page { get; init; }
        public int PageSize { get; init; }
        public int ApprovedCount { get; init; }
        public int PendingCount { get; init; }
        public int FlaggedCount { get; init; }
        public int DeclinedCount { get; init; }
        public List<IdeaRowViewModel> Items { get; init; } = new();
    }

    public async Task<(List<IdeaRowViewModel> Items, int Total, int Approved, int Pending, int Flagged, int Declined)>
        GetIdeasAsync(int projectId, string search, int? categoryId, string status, int page, int pageSize = 20)
    {
        var url = $"api/admin/projects/{projectId}/ideas?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(search))    url += $"&search={Uri.EscapeDataString(search)}";
        if (categoryId.HasValue)                   url += $"&categoryId={categoryId}";
        if (!string.IsNullOrWhiteSpace(status))    url += $"&status={Uri.EscapeDataString(status)}";

        var response = await httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
            return (new List<IdeaRowViewModel>(), 0, 0, 0, 0, 0);

        var result = await response.Content.ReadFromJsonAsync<IdeaPageResponse>()
                     ?? new IdeaPageResponse();

        return (result.Items, result.TotalCount, result.ApprovedCount, result.PendingCount, result.FlaggedCount, result.DeclinedCount);
    }

    public async Task SetStatusAsync(int projectId, int ideaId, string status)
    {
        var response = await httpClient.PatchAsJsonAsync(
            $"api/admin/projects/{projectId}/ideas/{ideaId}/status",
            new { Status = status });
        response.EnsureSuccessStatusCode();
    }

    public async Task SetCommentStatusAsync(int projectId, int ideaId, int commentId, string status)
    {
        var response = await httpClient.PatchAsJsonAsync(
            $"api/admin/projects/{projectId}/ideas/{ideaId}/comments/{commentId}/status",
            new { Status = status });
        response.EnsureSuccessStatusCode();
    }

    public async Task RemoveIdeaImageAsync(int projectId, int ideaId)
    {
        var response = await httpClient.DeleteAsync(
            $"api/admin/projects/{projectId}/ideas/{ideaId}/image");
        response.EnsureSuccessStatusCode();
    }

    public async Task<IdeaDetailViewModel> GetIdeaDetailAsync(int projectId, int ideaId)
    {
        var response = await httpClient.GetAsync($"api/admin/projects/{projectId}/ideas/{ideaId}");
        if (!response.IsSuccessStatusCode) return null;

        var dto = await response.Content.ReadFromJsonAsync<IdeaDetailResponse>();
        if (dto is null) return null;

        return new IdeaDetailViewModel
        {
            ProjectId   = projectId,
            IdeaId      = dto.IdeaId,
            Description = dto.Description,
            Status           = dto.Status,
            CreatedAt        = dto.CreatedAt,
            ParticipantEmail = dto.ParticipantEmail,
            ImageUrl         = dto.ImageUrl,
            LanguageCode     = dto.LanguageCode,
            EmojiReactions   = dto.EmojiReactions.Select(e => new IdeaEmojiReactionViewModel
            {
                Symbol = e.Symbol,
                Name   = e.Name,
                Count  = e.Count
            }).ToList(),
            Comments = dto.Comments.Select(c => new IdeaCommentViewModel
            {
                CommentId        = c.CommentId,
                Text             = c.Text,
                ModerationStatus = c.ModerationStatus,
                CreatedAt        = c.CreatedAt,
                ParticipantEmail = c.ParticipantEmail,
                LanguageCode     = c.LanguageCode,
                EmojiReactions   = c.EmojiReactions.Select(e => new IdeaEmojiReactionViewModel
                {
                    Symbol = e.Symbol,
                    Name   = e.Name,
                    Count  = e.Count
                }).ToList()
            }).ToList()
        };
    }

    private record IdeaDetailResponse
    {
        public int IdeaId { get; init; }
        public string Description { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
        public string ParticipantEmail { get; init; }
        public string ImageUrl { get; init; }
        public string LanguageCode { get; init; }
        public List<EmojiReactionResponse> EmojiReactions { get; init; } = new();
        public List<CommentResponse> Comments { get; init; } = new();
    }

    private record EmojiReactionResponse
    {
        public string Symbol { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public int Count { get; init; }
    }

    private record CommentResponse
    {
        public int CommentId { get; init; }
        public string Text { get; init; } = string.Empty;
        public string ModerationStatus { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
        public string ParticipantEmail { get; init; }
        public string LanguageCode { get; init; }
        public List<EmojiReactionResponse> EmojiReactions { get; init; } = new();
    }
}
