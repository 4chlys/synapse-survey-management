using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.Services;
using SynapseEnqueteManagement.BL.AiModeration;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/upload")]
public class UploadsController(
    IStorageService storageService,
    IAiModerationService aiModerationService,
    IModerationTokenService moderationTokenService) : ControllerBase
{
    private static readonly HashSet<string> AllowedImageContentTypes =
        ["image/jpeg", "image/png", "image/webp", "image/gif"];
    private const long MaxImageSizeBytes = 10 * 1024 * 1024; // 10 MB

    private static readonly HashSet<string> AllowedVideoContentTypes =
        ["video/mp4", "video/webm"];
    private const long MaxVideoSizeBytes = 25 * 1024 * 1024; // 25 MB

    private static readonly HashSet<string> AllowedAudioContentTypes =
        ["audio/mpeg", "audio/mp4", "audio/wav", "audio/ogg", "audio/x-m4a"];
    private const long MaxAudioSizeBytes = 20 * 1024 * 1024; // 20 MB

    private static readonly Dictionary<QuestionMediaType, (HashSet<string> Types, long MaxBytes, string Folder)>
        QuestionMediaConfig = new()
        {
            [QuestionMediaType.Image] = (AllowedImageContentTypes, 5 * 1024 * 1024, "questions/images"),
            [QuestionMediaType.Video] = (AllowedVideoContentTypes, MaxVideoSizeBytes, "questions/videos"),
            [QuestionMediaType.Audio] = (AllowedAudioContentTypes, MaxAudioSizeBytes, "questions/audios"),
        };

    [HttpPost]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<UploadResultDto>> Upload(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest("No file provided.");

        if (!AllowedImageContentTypes.Contains(file.ContentType))
            return BadRequest("Only image files (jpeg, png, webp, gif) are allowed.");

        if (file.Length > MaxImageSizeBytes)
            return BadRequest("File exceeds the 10 MB limit.");

        var url = await storageService.UploadAsync(file, "logos");
        return Ok(new UploadResultDto { Url = url });
    }

    [HttpPost("idea-image")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<IdeaImageUploadResultDto>> UploadIdeaImage(
        IFormFile file, [FromQuery] int projectId, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest("No file provided.");

        if (!AllowedImageContentTypes.Contains(file.ContentType))
            return BadRequest("Only image files (jpeg, png, webp, gif) are allowed.");

        if (file.Length > MaxImageSizeBytes)
            return BadRequest("File exceeds the 10 MB limit.");

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        var bytes = ms.ToArray();

        var modResult = await aiModerationService.CheckImageWithAiAsync(bytes, file.ContentType, ct, CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
        var imageStatus = modResult.IsAcceptable ? ModerationStatus.Approved : ModerationStatus.Flagged;

        // Always upload — token encodes the AI result so the idea can be stored as Flagged when rejected.
        // Images land in tmp/ideas/ so orphaned uploads are cleaned up by GCS lifecycle rules.
        var tmpUrl = await storageService.UploadAsync(file, "tmp/ideas");
        var imageModerationToken = moderationTokenService.GenerateImageToken(bytes, projectId, imageStatus);

        return Ok(new IdeaImageUploadResultDto
        {
            Url = tmpUrl,
            ImageModerationToken = imageModerationToken,
            IsAcceptable = modResult.IsAcceptable,
            Reason = modResult.IsAcceptable ? null : modResult.Reason
        });
    }

    [HttpPost("question-media")]
    [RequestSizeLimit(25 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 25 * 1024 * 1024)]
    public async Task<ActionResult<UploadResultDto>> UploadQuestionMedia(
        IFormFile file, [FromQuery] QuestionMediaType mediaType, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest("No file provided.");

        if (!QuestionMediaConfig.TryGetValue(mediaType, out var config))
            return BadRequest("Invalid media type.");

        if (!config.Types.Contains(file.ContentType))
            return BadRequest($"File type '{file.ContentType}' is not allowed for {mediaType}.");

        if (file.Length > config.MaxBytes)
            return BadRequest($"File exceeds the {config.MaxBytes / (1024 * 1024)} MB limit for {mediaType}.");

        var url = await storageService.UploadAsync(file, config.Folder);
        return Ok(new UploadResultDto { Url = url });
    }
}

public class UploadResultDto
{
    public string Url { get; set; }
}

public class IdeaImageUploadResultDto
{
    public string Url { get; set; }
    public string ImageModerationToken { get; set; }
    public bool IsAcceptable { get; set; }
    public string Reason { get; set; }
}
