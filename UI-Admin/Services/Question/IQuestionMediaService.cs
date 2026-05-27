using SynapseEnqueteManagement.UI.Admin.Models.Enums;

namespace SynapseEnqueteManagement.UI.Admin.Services.Question;

public interface IQuestionMediaService
{
    Task<QuestionMediaUploadResult> UploadAsync(IFormFile file, QuestionMediaType type, CancellationToken ct);
}

public record QuestionMediaUploadResult
{
    public bool Success { get; init; }
    public string Url { get; init; }
    public string Error { get; init; }
}
