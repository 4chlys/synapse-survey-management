using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.UI.Admin.Services.Question;

public interface IQuestionMediaService
{
    Task<QuestionMediaUploadResult> UploadAsync(IFormFile file, QuestionMediaType type, CancellationToken ct);
}

public class QuestionMediaUploadResult
{
    public bool Success { get; set; }
    public string Url { get; set; }
    public string Error { get; set; }
}
